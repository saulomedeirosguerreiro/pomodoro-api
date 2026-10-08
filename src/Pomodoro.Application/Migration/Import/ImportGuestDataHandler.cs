using System.Text.Json;
using FluentValidation;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Achievements;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Pomodoros;
using Pomodoro.Application.Pomodoros.Create;
using Pomodoro.Application.Tasks.Create;
using Pomodoro.Domain.Achievements;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Application.Migration.Import;

/// <summary>
/// Importa em lote tarefas/sessões do modo sem conta (localStorage) para a conta recém-criada/logada
/// (D1-D6 do plano técnico): atômica (tudo ou nada, via <see cref="IUnitOfWork"/>), idempotente por
/// (UserId, GuestId), reaproveita os MESMOS validators/regras de <see cref="CreateTaskHandler"/>/
/// <see cref="CreatePomodoroHandler"/> (nunca duplica antifraude), e data conquistas importadas na data
/// histórica real do evento (<see cref="AchievementTimelineResolver"/>), não na data da importação.
/// </summary>
public sealed class ImportGuestDataHandler
{
    private const int FutureToleranceSeconds = 60;

    private readonly ITaskRepository _tasks;
    private readonly IPomodoroSessionRepository _sessions;
    private readonly IAchievementRepository _achievements;
    private readonly IGuestImportRepository _imports;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly AchievementStatsProvider _statsProvider;
    private readonly IValidator<CreateTaskRequest> _taskValidator;
    private readonly IValidator<CreatePomodoroRequest> _sessionValidator;

    public ImportGuestDataHandler(
        ITaskRepository tasks,
        IPomodoroSessionRepository sessions,
        IAchievementRepository achievements,
        IGuestImportRepository imports,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        AchievementStatsProvider statsProvider,
        IValidator<CreateTaskRequest> taskValidator,
        IValidator<CreatePomodoroRequest> sessionValidator)
    {
        _tasks = tasks;
        _sessions = sessions;
        _achievements = achievements;
        _imports = imports;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _statsProvider = statsProvider;
        _taskValidator = taskValidator;
        _sessionValidator = sessionValidator;
    }

    public async Task<ImportGuestDataResponse> HandleAsync(
        int userId, ImportGuestDataRequest request, CancellationToken cancellationToken)
    {
        var existing = await _imports.FindAsync(userId, request.GuestId, cancellationToken);
        if (existing is not null)
        {
            return BuildResponseFromRecord(existing);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var localIdToTaskId = new Dictionary<string, int>();
            var taskCompletionTimestamps = new List<DateTime>();
            var skipped = new List<ImportSkippedItem>();

            await ImportTasksAsync(userId, request.Tasks, localIdToTaskId, taskCompletionTimestamps, skipped, cancellationToken);
            var sessionsImported = await ImportSessionsAsync(
                userId, request.Sessions, localIdToTaskId, skipped, cancellationToken);

            var achievementsUnlocked = await CreditHistoricalAchievementsAsync(
                userId, taskCompletionTimestamps, cancellationToken);

            var record = GuestImport.Create(
                userId, request.GuestId, _clock.UtcNow,
                localIdToTaskId.Count, sessionsImported,
                JsonSerializer.Serialize(achievementsUnlocked), JsonSerializer.Serialize(skipped));

            await _imports.AddAsync(record, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new ImportGuestDataResponse(
                record.Id, record.GuestId, record.ImportedAt,
                record.TasksImported, record.SessionsImported, achievementsUnlocked, skipped);
        }
        catch (DuplicateGuestImportException)
        {
            // Corrida: outra requisição com o mesmo (userId, guestId) venceu e já commitou.
            await transaction.RollbackAsync(cancellationToken);
            var winner = await _imports.FindAsync(userId, request.GuestId, cancellationToken);
            return BuildResponseFromRecord(winner!); // nunca null: a violação só ocorre se o outro já commitou
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ImportTasksAsync(
        int userId,
        IReadOnlyList<GuestTaskImportItem> items,
        Dictionary<string, int> localIdToTaskId,
        List<DateTime> taskCompletionTimestamps,
        List<ImportSkippedItem> skipped,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var mapped = new CreateTaskRequest(item.Title, item.Description, item.Priority, item.EstimatedPomodoros);
            var validation = await _taskValidator.ValidateAsync(mapped, cancellationToken);
            if (!validation.IsValid)
            {
                skipped.Add(new ImportSkippedItem("task", item.LocalId, validation.Errors[0].ErrorMessage));
                continue;
            }

            if (!TaskWireFormat.TryParseStatus(item.Status, out var status))
            {
                skipped.Add(new ImportSkippedItem("task", item.LocalId, "Status de tarefa inválido."));
                continue;
            }

            try
            {
                TaskWireFormat.TryParsePriority(item.Priority, out var priority);
                var createdAt = item.CreatedAt.HasValue ? ClampFuture(item.CreatedAt.Value) : _clock.UtcNow;
                var task = TaskItem.Create(userId, item.Title, item.Description, priority, item.EstimatedPomodoros, createdAt);

                if (status == TaskItemStatus.EmCurso)
                {
                    var currentlyFocused = await _tasks.FindInFocusAsync(userId, cancellationToken);
                    if (currentlyFocused is not null)
                    {
                        currentlyFocused.MarkAsTodo(createdAt);
                    }

                    task.MarkAsFocused(createdAt);
                }
                else if (status == TaskItemStatus.Feito)
                {
                    var completedAt = item.CompletedAt.HasValue ? ClampFuture(item.CompletedAt.Value) : createdAt;
                    task.MarkAsDone(completedAt);
                    taskCompletionTimestamps.Add(completedAt);
                }

                await _tasks.AddAsync(task, cancellationToken);
                localIdToTaskId[item.LocalId] = task.Id;
            }
            catch (Domain.Common.DomainException ex)
            {
                skipped.Add(new ImportSkippedItem("task", item.LocalId, ex.Message));
            }
        }
    }

    private async Task<int> ImportSessionsAsync(
        int userId,
        IReadOnlyList<GuestSessionImportItem> items,
        Dictionary<string, int> localIdToTaskId,
        List<ImportSkippedItem> skipped,
        CancellationToken cancellationToken)
    {
        var imported = 0;

        foreach (var item in items)
        {
            int? resolvedTaskId = null;
            if (item.TaskLocalId is not null)
            {
                if (!localIdToTaskId.TryGetValue(item.TaskLocalId, out var taskId))
                {
                    skipped.Add(new ImportSkippedItem(
                        "session", item.LocalId, $"Tarefa local '{item.TaskLocalId}' vinculada não pôde ser importada."));
                    continue;
                }

                resolvedTaskId = taskId;
            }

            var mapped = new CreatePomodoroRequest(
                item.Type, item.Status, item.DurationSeconds, item.StartedAt, item.CompletedAt, resolvedTaskId);
            var validation = await _sessionValidator.ValidateAsync(mapped, cancellationToken);
            if (!validation.IsValid)
            {
                skipped.Add(new ImportSkippedItem("session", item.LocalId, validation.Errors[0].ErrorMessage));
                continue;
            }

            var overlaps = await _sessions.ExistsOverlappingAsync(userId, item.StartedAt, item.CompletedAt, cancellationToken);
            if (overlaps)
            {
                skipped.Add(new ImportSkippedItem(
                    "session", item.LocalId, "Esta sessão se sobrepõe a outra já registrada."));
                continue;
            }

            try
            {
                SessionWireFormat.TryParseType(item.Type, out var type);
                SessionWireFormat.TryParseStatus(item.Status, out var status);

                if (resolvedTaskId.HasValue)
                {
                    await TaskLinkGuard.EnsureCanBeLinkedAsync(_tasks, userId, resolvedTaskId.Value, type, cancellationToken);
                }

                var createdAt = item.StartedAt.Kind != DateTimeKind.Unspecified ? item.CompletedAt : _clock.UtcNow;
                var session = PomodoroSession.Create(
                    userId, type, status, item.DurationSeconds, item.StartedAt, item.CompletedAt, createdAt, resolvedTaskId);

                await _sessions.AddAsync(session, cancellationToken);
                imported++;
            }
            catch (FieldValidationException ex)
            {
                skipped.Add(new ImportSkippedItem("session", item.LocalId, ex.Message));
            }
            catch (Domain.Common.DomainException ex)
            {
                skipped.Add(new ImportSkippedItem("session", item.LocalId, ex.Message));
            }
        }

        return imported;
    }

    private async Task<List<ImportedAchievementSummary>> CreditHistoricalAchievementsAsync(
        int userId, List<DateTime> newlyCompletedTaskTimestamps, CancellationToken cancellationToken)
    {
        var stats = await _statsProvider.BuildAsync(userId, cancellationToken);
        var alreadyUnlocked = (await _achievements.ListForUserAsync(userId, cancellationToken))
            .Select(a => a.Code)
            .ToHashSet();

        var newDefinitions = AchievementCatalog.All
            .Where(d => !alreadyUnlocked.Contains(d.Code) && d.IsUnlocked(stats))
            .ToList();

        var result = new List<ImportedAchievementSummary>();
        if (newDefinitions.Count == 0)
        {
            return result;
        }

        var allSessions = (await _sessions.ListAllAsync(userId, cancellationToken))
            .OrderBy(s => s.CompletedAt)
            .ToList();

        var existingDoneTasks = await _tasks.ListForUserAsync(userId, TaskItemStatus.Feito, cancellationToken);
        var taskCompletionTimestamps = existingDoneTasks
            .Select(t => t.UpdatedAt)
            .Concat(newlyCompletedTaskTimestamps)
            .OrderBy(t => t)
            .ToList();

        foreach (var definition in newDefinitions)
        {
            var unlockMoment = AchievementTimelineResolver.FindUnlockMoment(
                allSessions, taskCompletionTimestamps, stats.CompletedTasksCount, definition.IsUnlocked);

            // Nunca null aqui: `definition` só entra em `newDefinitions` porque `definition.IsUnlocked(stats)`
            // já é true nos stats finais, e `BuildStatsAt` no último índice da timeline usa a MESMA fórmula
            // (mesmas sessões, mesma contagem final de tarefas) que produziu `stats` — logo o prefixo completo
            // da timeline satisfaz o predicado por construção (ver AchievementTimelineResolver.FindUnlockMoment).
            var unlockedAt = ClampFuture(unlockMoment!.Value);

            try
            {
                await _achievements.AddAsync(UserAchievement.Create(userId, definition.Code, unlockedAt), cancellationToken);
                result.Add(new ImportedAchievementSummary(definition.Code, definition.Name, unlockedAt));
            }
            catch (DuplicateUserAchievementException)
            {
                // Já desbloqueada concorrentemente (ex.: outra aba registrando uma sessão em paralelo) — ignora.
            }
        }

        return result;
    }

    private DateTime ClampFuture(DateTime value) =>
        value <= _clock.UtcNow.AddSeconds(FutureToleranceSeconds) ? value : _clock.UtcNow;

    // O `!` abaixo é seguro: os dois campos JSON só são produzidos por `JsonSerializer.Serialize` a partir
    // de listas (nunca nulas) em `HandleAsync` — na pior das hipóteses desserializam para uma lista vazia
    // (JSON "[]"), nunca para `null` (que só aconteceria a partir do literal JSON "null").
    private static ImportGuestDataResponse BuildResponseFromRecord(GuestImport record) => new(
        record.Id,
        record.GuestId,
        record.ImportedAt,
        record.TasksImported,
        record.SessionsImported,
        JsonSerializer.Deserialize<List<ImportedAchievementSummary>>(record.AchievementsUnlockedJson)!,
        JsonSerializer.Deserialize<List<ImportSkippedItem>>(record.SkippedItemsJson)!);
}
