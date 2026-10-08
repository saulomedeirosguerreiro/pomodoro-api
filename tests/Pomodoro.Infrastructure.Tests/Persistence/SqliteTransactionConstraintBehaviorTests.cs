using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Persistence;
using Pomodoro.Infrastructure.Repositories;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Persistence;

/// <summary>
/// Prova empírica (não assumida de memória) de uma premissa de que a importação em lote (F2) vai
/// depender: no SQLite, uma violação de constraint única NO MEIO de uma transação aborta só a
/// instrução que violou, não a transação inteira — diferente de bancos com semântica estrita de
/// "transação envenenada" após qualquer erro. Ver seção 1.7 do plano técnico do backend.
/// </summary>
public class SqliteTransactionConstraintBehaviorTests : IDisposable
{
    private readonly SqliteInMemoryContextFactory _factory = new();
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ViolacaoDeIndiceUnicoDentroDaTransacao_AbortaSoAInstrucao_TransacaoContinuaAbertaEComitavel()
    {
        using var context = _factory.CreateContext();
        await new UserRepository(context).AddAsync(
            User.Create("Ana", "ana@email.com", "hash", BaseTime), CancellationToken.None);

        var unitOfWork = new EfUnitOfWork(context);
        var achievements = new AchievementRepository(context);

        await using var transaction = await unitOfWork.BeginTransactionAsync(CancellationToken.None);

        await achievements.AddAsync(UserAchievement.Create(1, "primeira_semente", BaseTime), CancellationToken.None);

        var duplicate = UserAchievement.Create(1, "primeira_semente", BaseTime);
        var act = async () => await achievements.AddAsync(duplicate, CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();

        // O EF mantém a entidade que falhou rastreada como "Added"; sem desanexá-la, o próximo
        // SaveChangesAsync tentaria regravá-la e falharia de novo. Isso é do change tracker do EF,
        // não da transação SQLite em si — o que este teste prova é que a TRANSAÇÃO continua aberta
        // e aceita trabalho novo depois da violação.
        context.Entry(duplicate).State = EntityState.Detached;

        await achievements.AddAsync(UserAchievement.Create(1, "cem_tomates", BaseTime), CancellationToken.None);

        await transaction.CommitAsync(CancellationToken.None);

        using var verifyContext = _factory.CreateContext();
        var codes = await verifyContext.UserAchievements
            .Where(a => a.UserId == 1)
            .Select(a => a.Code)
            .ToListAsync();
        codes.Should().BeEquivalentTo(new[] { "primeira_semente", "cem_tomates" });
    }
}
