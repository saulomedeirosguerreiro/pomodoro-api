using FluentAssertions;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Pomodoro.Infrastructure.Repositories;
using Pomodoro.Infrastructure.Tests.Persistence;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Repositories;

public class PomodoroSessionRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryContextFactory _factory = new();
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public void Dispose() => _factory.Dispose();

    private static PomodoroSession BuildSession(
        int userId, SessionType type, SessionStatus status, DateTime createdAt) => PomodoroSession.Create(
        userId, type, status, SessionTypeDurations.StandardSecondsFor(type),
        createdAt.AddSeconds(-SessionTypeDurations.StandardSecondsFor(type)), createdAt, createdAt);

    /// <summary>Semeia usuários 1..count (FK de PomodoroSession exige um User existente).</summary>
    private async Task SeedUsersAsync(int count)
    {
        using var context = _factory.CreateContext();
        var repository = new UserRepository(context);
        for (var i = 0; i < count; i++)
        {
            await repository.AddAsync(User.Create($"Usuário {i + 1}", $"user{i + 1}@email.com", "hash", BaseTime), CancellationToken.None);
        }
    }

    [Fact]
    public async Task AddAsync_PersisteEAtribuiId()
    {
        await SeedUsersAsync(1);
        using var context = _factory.CreateContext();
        var repository = new PomodoroSessionRepository(context);
        var session = BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime);

        await repository.AddAsync(session, CancellationToken.None);

        session.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdForUserAsync_ComSessaoDoDono_Retorna()
    {
        await SeedUsersAsync(1);
        int id;
        using (var seedContext = _factory.CreateContext())
        {
            var session = BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime);
            await new PomodoroSessionRepository(seedContext).AddAsync(session, CancellationToken.None);
            id = session.Id;
        }

        using var context = _factory.CreateContext();
        var found = await new PomodoroSessionRepository(context).GetByIdForUserAsync(id, 1, CancellationToken.None);

        found.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdForUserAsync_ComSessaoDeOutroUsuario_RetornaNull()
    {
        await SeedUsersAsync(1);
        int id;
        using (var seedContext = _factory.CreateContext())
        {
            var session = BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime);
            await new PomodoroSessionRepository(seedContext).AddAsync(session, CancellationToken.None);
            id = session.Id;
        }

        using var context = _factory.CreateContext();
        var found = await new PomodoroSessionRepository(context).GetByIdForUserAsync(id, 2, CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdForUserAsync_ComIdInexistente_RetornaNull()
    {
        using var context = _factory.CreateContext();
        var found = await new PomodoroSessionRepository(context).GetByIdForUserAsync(999, 1, CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task ListForUserAsync_RetornaApenasDoUsuarioOrdenadoPorMaisRecenteComPaginacao()
    {
        await SeedUsersAsync(2);
        using (var seedContext = _factory.CreateContext())
        {
            var repo = new PomodoroSessionRepository(seedContext);
            await repo.AddAsync(BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime), CancellationToken.None);
            await repo.AddAsync(BuildSession(1, SessionType.DescansoCurto, SessionStatus.Concluido, BaseTime.AddMinutes(1)), CancellationToken.None);
            await repo.AddAsync(BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime.AddMinutes(2)), CancellationToken.None);
            await repo.AddAsync(BuildSession(2, SessionType.Foco, SessionStatus.Concluido, BaseTime.AddMinutes(3)), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var (items, totalCount) = await new PomodoroSessionRepository(context)
            .ListForUserAsync(userId: 1, limit: 2, offset: 0, CancellationToken.None);

        totalCount.Should().Be(3);
        items.Should().HaveCount(2);
        items[0].CreatedAt.Should().Be(BaseTime.AddMinutes(2));
        items[1].CreatedAt.Should().Be(BaseTime.AddMinutes(1));
    }

    [Fact]
    public async Task ListForUserAsync_ComOffset_PulaOsMaisRecentes()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            var repo = new PomodoroSessionRepository(seedContext);
            await repo.AddAsync(BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime), CancellationToken.None);
            await repo.AddAsync(BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime.AddMinutes(1)), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var (items, _) = await new PomodoroSessionRepository(context)
            .ListForUserAsync(userId: 1, limit: 10, offset: 1, CancellationToken.None);

        items.Should().ContainSingle();
        items[0].CreatedAt.Should().Be(BaseTime);
    }

    [Fact]
    public async Task CountCompletedFocusAsync_ContaSoFocoConcluidoDoUsuario()
    {
        await SeedUsersAsync(2);
        using (var seedContext = _factory.CreateContext())
        {
            var repo = new PomodoroSessionRepository(seedContext);
            await repo.AddAsync(BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime), CancellationToken.None);
            await repo.AddAsync(BuildSession(1, SessionType.Foco, SessionStatus.Interrompido, BaseTime.AddMinutes(1)), CancellationToken.None);
            await repo.AddAsync(BuildSession(1, SessionType.DescansoCurto, SessionStatus.Concluido, BaseTime.AddMinutes(2)), CancellationToken.None);
            await repo.AddAsync(BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime.AddMinutes(3)), CancellationToken.None);
            await repo.AddAsync(BuildSession(2, SessionType.Foco, SessionStatus.Concluido, BaseTime.AddMinutes(4)), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var count = await new PomodoroSessionRepository(context).CountCompletedFocusAsync(1, CancellationToken.None);

        count.Should().Be(2);
    }

    [Fact]
    public async Task ExistsOverlappingAsync_ComIntervaloQueSeSobrepoe_RetornaTrue()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            // 10:00 - 10:25
            await new PomodoroSessionRepository(seedContext).AddAsync(
                BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime.AddMinutes(25)), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var overlaps = await new PomodoroSessionRepository(context).ExistsOverlappingAsync(
            1, BaseTime.AddMinutes(20), BaseTime.AddMinutes(45), CancellationToken.None);

        overlaps.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsOverlappingAsync_ComIntervaloAdjacenteSemSobreposicao_RetornaFalse()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            // 10:00 - 10:25
            await new PomodoroSessionRepository(seedContext).AddAsync(
                BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime.AddMinutes(25)), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var overlaps = await new PomodoroSessionRepository(context).ExistsOverlappingAsync(
            1, BaseTime.AddMinutes(25), BaseTime.AddMinutes(50), CancellationToken.None);

        overlaps.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsOverlappingAsync_ComSessaoSobrepostaDeOutroUsuario_RetornaFalse()
    {
        await SeedUsersAsync(2);
        using (var seedContext = _factory.CreateContext())
        {
            await new PomodoroSessionRepository(seedContext).AddAsync(
                BuildSession(2, SessionType.Foco, SessionStatus.Concluido, BaseTime.AddMinutes(25)), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var overlaps = await new PomodoroSessionRepository(context).ExistsOverlappingAsync(
            1, BaseTime, BaseTime.AddMinutes(25), CancellationToken.None);

        overlaps.Should().BeFalse();
    }

    [Fact]
    public async Task ListAllAsync_RetornaTodasAsSessoesDoUsuarioSemPaginacao()
    {
        await SeedUsersAsync(2);
        using (var seedContext = _factory.CreateContext())
        {
            var repo = new PomodoroSessionRepository(seedContext);
            for (var i = 0; i < 15; i++)
            {
                await repo.AddAsync(BuildSession(1, SessionType.Foco, SessionStatus.Concluido, BaseTime.AddMinutes(i)), CancellationToken.None);
            }
            await repo.AddAsync(BuildSession(2, SessionType.Foco, SessionStatus.Concluido, BaseTime), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var all = await new PomodoroSessionRepository(context).ListAllAsync(1, CancellationToken.None);

        all.Should().HaveCount(15);
        all.Should().OnlyContain(s => s.UserId == 1);
    }

    private static PomodoroSession BuildFocusSessionForTask(
        int userId, SessionStatus status, int? taskItemId, DateTime createdAt) => PomodoroSession.Create(
        userId, SessionType.Foco, status, SessionTypeDurations.FocoSeconds,
        createdAt.AddSeconds(-SessionTypeDurations.FocoSeconds), createdAt, createdAt, taskItemId);

    [Fact]
    public async Task CountCompletedByTaskAsync_ComListaVazia_RetornaDicionarioVazio()
    {
        using var context = _factory.CreateContext();
        var result = await new PomodoroSessionRepository(context)
            .CountCompletedByTaskAsync(Array.Empty<int>(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CountCompletedByTaskAsync_ContaSoFocosConcluidosPorTarefa()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            var taskRepo = new TaskRepository(seedContext);
            var taskA = TaskItem.Create(1, "Tarefa A", null, TaskPriority.Media, 4, BaseTime);
            var taskB = TaskItem.Create(1, "Tarefa B", null, TaskPriority.Media, 4, BaseTime);
            await taskRepo.AddAsync(taskA, CancellationToken.None);
            await taskRepo.AddAsync(taskB, CancellationToken.None);

            var repo = new PomodoroSessionRepository(seedContext);
            await repo.AddAsync(BuildFocusSessionForTask(1, SessionStatus.Concluido, taskA.Id, BaseTime), CancellationToken.None);
            await repo.AddAsync(BuildFocusSessionForTask(1, SessionStatus.Concluido, taskA.Id, BaseTime.AddMinutes(1)), CancellationToken.None);
            await repo.AddAsync(BuildFocusSessionForTask(1, SessionStatus.Interrompido, taskA.Id, BaseTime.AddMinutes(2)), CancellationToken.None);
            await repo.AddAsync(BuildFocusSessionForTask(1, SessionStatus.Concluido, taskB.Id, BaseTime.AddMinutes(3)), CancellationToken.None);
            await repo.AddAsync(BuildFocusSessionForTask(1, SessionStatus.Concluido, null, BaseTime.AddMinutes(4)), CancellationToken.None);

            using var context = _factory.CreateContext();
            var result = await new PomodoroSessionRepository(context)
                .CountCompletedByTaskAsync(new[] { taskA.Id, taskB.Id }, CancellationToken.None);

            result[taskA.Id].Should().Be(2);
            result[taskB.Id].Should().Be(1);
        }
    }
}
