using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Api.Seed;

/// <summary>
/// Usuário de teste pronto para avaliação (US-25, US-64). Idempotente: roda quantas vezes quiser.
/// Cobre, de propósito, as telas gamificadas: streak &gt; 0 (3 dias seguidos), tarefas nos três
/// status (uma delas em foco) e pelo menos uma conquista já desbloqueada.
/// </summary>
public static class SeedData
{
    public const string TestUserEmail = "teste@pomodoro.com";
    public const string TestUserPassword = "Teste123";

    public static async Task RunAsync(IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        if (await users.ExistsByEmailAsync(TestUserEmail, CancellationToken.None))
        {
            Console.WriteLine($"Usuário de teste '{TestUserEmail}' já existe. Nada a fazer.");
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var sessions = scope.ServiceProvider.GetRequiredService<IPomodoroSessionRepository>();
        var tasks = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var achievementEvaluator = scope.ServiceProvider.GetRequiredService<IAchievementEvaluator>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var passwordHash = hasher.Hash(TestUserPassword);
        var user = User.Create("Usuário de Teste", TestUserEmail, passwordHash, clock.UtcNow);
        await users.AddAsync(user, CancellationToken.None);

        foreach (var session in BuildExampleSessions(user.Id, clock.UtcNow))
        {
            await sessions.AddAsync(session, CancellationToken.None);
        }

        await SeedTasksAsync(tasks, user.Id, clock.UtcNow);

        // US-53 RN-02: mesma avaliação usada em produção — "Primeira Semente" e "Primeira Colheita"
        // nascem desbloqueadas a partir dos dados acima, sem inserir conquista "na mão".
        await achievementEvaluator.HandleAsync(user.Id, CancellationToken.None);

        Console.WriteLine($"Usuário de teste criado: {TestUserEmail} / {TestUserPassword}");
    }

    private static IEnumerable<PomodoroSession> BuildExampleSessions(int userId, DateTime utcNow)
    {
        // Âncora sempre no passado (nunca no "agora"), com os outros dois dias exatamente 24h antes
        // dela — como o fuso default de US-40 (America/Sao_Paulo) tem offset fixo (sem DST), três
        // instantes exatamente 24h apartados sempre caem em três dias civis consecutivos distintos.
        var anchor = utcNow.AddHours(-1);

        // 3 dias seguidos com foco concluído => streak > 0 (US-40/G-Q10), idêntico à forma como
        // um usuário real acumularia streak, só que retroativo.
        foreach (var day in new[] { anchor.AddDays(-2), anchor.AddDays(-1), anchor })
        {
            var focoEnd = day;
            var focoStart = focoEnd.AddSeconds(-SessionTypeDurations.FocoSeconds);
            yield return PomodoroSession.Create(
                userId, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
                focoStart, focoEnd, focoEnd);

            var pausaStart = focoEnd;
            var pausaEnd = pausaStart.AddSeconds(SessionTypeDurations.DescansoCurtoSeconds);
            yield return PomodoroSession.Create(
                userId, SessionType.DescansoCurto, SessionStatus.Concluido, SessionTypeDurations.DescansoCurtoSeconds,
                pausaStart, pausaEnd, pausaEnd);
        }

        // Um exemplo de sessão interrompida, para a tela de histórico não mostrar só "Concluído".
        var interrompidoStart = anchor.AddHours(2);
        var interrompidoEnd = interrompidoStart.AddSeconds(600);
        yield return PomodoroSession.Create(
            userId, SessionType.Foco, SessionStatus.Interrompido, 600,
            interrompidoStart, interrompidoEnd, interrompidoEnd);
    }

    private static async Task SeedTasksAsync(ITaskRepository tasks, int userId, DateTime utcNow)
    {
        var aFazer = TaskItem.Create(userId, "Planejar a semana", "Revisar prioridades no início do expediente.", TaskPriority.Media, 2, utcNow);
        await tasks.AddAsync(aFazer, CancellationToken.None);

        var emFoco = TaskItem.Create(userId, "Escrever o relatório mensal", "Fechar os números do mês e enviar ao time.", TaskPriority.Alta, 4, utcNow);
        emFoco.MarkAsFocused(utcNow);
        await tasks.AddAsync(emFoco, CancellationToken.None);

        var feita = TaskItem.Create(userId, "Responder e-mails pendentes", null, TaskPriority.Baixa, 1, utcNow);
        feita.MarkAsDone(utcNow);
        await tasks.AddAsync(feita, CancellationToken.None);
    }
}
