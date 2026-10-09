using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Persistence;
using Pomodoro.Infrastructure.Repositories;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Persistence;

/// <summary>
/// Prova de que o SAVEPOINT explícito em <see cref="AchievementRepository.AddAsync"/> e
/// <see cref="GuestImportRepository.AddAsync"/> reproduz, no Postgres, a mesma garantia que o SQLite
/// dava de graça: uma violação de constraint única NO MEIO de uma transação aborta só a instrução
/// que violou, não a transação inteira. O Postgres, ao contrário do SQLite, teria por padrão a
/// transação inteira "envenenada" após qualquer erro — sem o SAVEPOINT explícito (e o
/// RollbackToSavepointAsync no catch), o próximo comando na mesma transação falharia com
/// "current transaction is aborted". Ver seção 1.7 do plano técnico do backend e achados A/B do
/// plano de migração para Postgres.
/// </summary>
public class PostgresSavepointConstraintBehaviorTests : IDisposable
{
    private readonly PostgresTestDatabaseFactory _factory = new();
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ViolacaoDeIndiceUnicoDentroDaTransacao_AbortaSoAInstrucao_TransacaoContinuaAbertaEComitavel_Achievements()
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
        await act.Should().ThrowAsync<DuplicateUserAchievementException>();

        // Sem desanexar nada aqui à mão: o próprio repositório desanexa a entidade que falhou no
        // catch (achado B) — diferente do teste original em SQLite, que precisava fazer isso manualmente.
        await achievements.AddAsync(UserAchievement.Create(1, "cem_tomates", BaseTime), CancellationToken.None);

        await transaction.CommitAsync(CancellationToken.None);

        using var verifyContext = _factory.CreateContext();
        var codes = await verifyContext.UserAchievements
            .Where(a => a.UserId == 1)
            .Select(a => a.Code)
            .ToListAsync();
        codes.Should().BeEquivalentTo(new[] { "primeira_semente", "cem_tomates" });
    }

    [Fact]
    public async Task ViolacaoDeIndiceUnicoDentroDaTransacao_AbortaSoAInstrucao_TransacaoContinuaAbertaEComitavel_GuestImports()
    {
        using var context = _factory.CreateContext();
        await new UserRepository(context).AddAsync(
            User.Create("Ana", "ana@email.com", "hash", BaseTime), CancellationToken.None);

        var unitOfWork = new EfUnitOfWork(context);
        var guestImports = new GuestImportRepository(context);

        await using var transaction = await unitOfWork.BeginTransactionAsync(CancellationToken.None);

        await guestImports.AddAsync(GuestImport.Create(1, "guest-1", BaseTime, 2, 3, "[]", "[]"), CancellationToken.None);

        var duplicate = GuestImport.Create(1, "guest-1", BaseTime, 9, 9, "[]", "[]");
        var act = async () => await guestImports.AddAsync(duplicate, CancellationToken.None);
        await act.Should().ThrowAsync<DuplicateGuestImportException>();

        await guestImports.AddAsync(GuestImport.Create(1, "guest-2", BaseTime, 1, 1, "[]", "[]"), CancellationToken.None);

        await transaction.CommitAsync(CancellationToken.None);

        using var verifyContext = _factory.CreateContext();
        var guestIds = await verifyContext.GuestImports
            .Where(g => g.UserId == 1)
            .Select(g => g.GuestId)
            .ToListAsync();
        guestIds.Should().BeEquivalentTo(new[] { "guest-1", "guest-2" });
    }
}
