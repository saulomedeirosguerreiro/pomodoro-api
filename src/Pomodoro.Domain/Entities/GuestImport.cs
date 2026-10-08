using Pomodoro.Domain.Common;

namespace Pomodoro.Domain.Entities;

/// <summary>
/// Registro de idempotência de uma importação em lote do modo sem conta (D2): o par (UserId, GuestId)
/// é único — um replay do mesmo guestId nunca reprocessa, só devolve o relatório já salvo. Os relatórios
/// ficam serializados como JSON (texto) de propósito: só são lidos de volta no replay do mesmo guestId,
/// nunca filtrados/agregados por query — não vale o custo de tabelas filho normalizadas para isso (KISS).
/// </summary>
public sealed class GuestImport
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string GuestId { get; private set; } = string.Empty;
    public DateTime ImportedAt { get; private set; }
    public int TasksImported { get; private set; }
    public int SessionsImported { get; private set; }
    public string AchievementsUnlockedJson { get; private set; } = "[]";
    public string SkippedItemsJson { get; private set; } = "[]";

    private GuestImport()
    {
    }

    public static GuestImport Create(
        int userId,
        string guestId,
        DateTime utcNow,
        int tasksImported,
        int sessionsImported,
        string achievementsUnlockedJson,
        string skippedItemsJson)
    {
        if (userId <= 0)
        {
            throw new DomainException("Importação precisa pertencer a um usuário válido.");
        }

        if (string.IsNullOrWhiteSpace(guestId))
        {
            throw new DomainException("GuestId é obrigatório.");
        }

        // Demais campos são resultado de cálculo interno do handler, não entrada de usuário — sem validação extra.
        return new GuestImport
        {
            UserId = userId,
            GuestId = guestId.Trim(),
            ImportedAt = utcNow,
            TasksImported = tasksImported,
            SessionsImported = sessionsImported,
            AchievementsUnlockedJson = achievementsUnlockedJson,
            SkippedItemsJson = skippedItemsJson,
        };
    }
}
