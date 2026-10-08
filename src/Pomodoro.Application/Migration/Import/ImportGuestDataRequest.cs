namespace Pomodoro.Application.Migration.Import;

/// <summary>
/// Lote de dados do modo sem conta (localStorage) a importar para a conta recém-criada/logada.
/// `UserId` nunca é aceito aqui: o dono da importação vem sempre do token (L-13), igual ao restante da Api.
/// </summary>
public sealed record ImportGuestDataRequest(
    string GuestId,
    IReadOnlyList<GuestTaskImportItem> Tasks,
    IReadOnlyList<GuestSessionImportItem> Sessions);

/// <summary>
/// `LocalId` é o id gerado no navegador (ex.: uuid) — nunca um id de banco, já que tarefas do guest
/// nunca tiveram um. `CompletedAt` só é usado (D5) para datar a conquista "Primeira Colheita" quando
/// `Status == "feito"`; não é persistido em `TaskItem` (que não tem coluna de data de conclusão).
/// </summary>
public sealed record GuestTaskImportItem(
    string LocalId,
    string Title,
    string? Description,
    string Priority,
    int EstimatedPomodoros,
    string Status,
    DateTime? CreatedAt,
    DateTime? CompletedAt);

/// <summary>`TaskLocalId` referencia `GuestTaskImportItem.LocalId` no MESMO payload — nunca um id de banco.</summary>
public sealed record GuestSessionImportItem(
    string LocalId,
    string Type,
    string Status,
    int DurationSeconds,
    DateTime StartedAt,
    DateTime CompletedAt,
    string? TaskLocalId);
