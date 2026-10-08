using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Persistence;
using Pomodoro.Infrastructure.Repositories;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Persistence;

public class EfUnitOfWorkTests : IDisposable
{
    private readonly SqliteInMemoryContextFactory _factory = new();
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CommitAsync_PersisteAlteracoesFeitasPorDoisRepositoriosNaMesmaTransacao()
    {
        using var context = _factory.CreateContext();
        var unitOfWork = new EfUnitOfWork(context);
        var users = new UserRepository(context);
        var achievements = new AchievementRepository(context);

        await using (var transaction = await unitOfWork.BeginTransactionAsync(CancellationToken.None))
        {
            var user = User.Create("Ana", "ana@email.com", "hash", BaseTime);
            await users.AddAsync(user, CancellationToken.None);
            await achievements.AddAsync(
                UserAchievement.Create(user.Id, "primeira_semente", BaseTime), CancellationToken.None);

            await transaction.CommitAsync(CancellationToken.None);
        }

        using var verifyContext = _factory.CreateContext();
        (await verifyContext.Users.CountAsync()).Should().Be(1);
        (await verifyContext.UserAchievements.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RollbackAsync_NaoPersisteNadaDoQueFoiSalvoDentroDaTransacao()
    {
        using var context = _factory.CreateContext();
        var unitOfWork = new EfUnitOfWork(context);
        var users = new UserRepository(context);
        var achievements = new AchievementRepository(context);

        await using (var transaction = await unitOfWork.BeginTransactionAsync(CancellationToken.None))
        {
            var user = User.Create("Ana", "ana@email.com", "hash", BaseTime);
            await users.AddAsync(user, CancellationToken.None);
            await achievements.AddAsync(
                UserAchievement.Create(user.Id, "primeira_semente", BaseTime), CancellationToken.None);

            await transaction.RollbackAsync(CancellationToken.None);
        }

        using var verifyContext = _factory.CreateContext();
        (await verifyContext.Users.CountAsync()).Should().Be(0);
        (await verifyContext.UserAchievements.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DisposeAsyncSemCommitNemRollback_NaoPersisteAlteracoesFeitasDentroDaTransacao()
    {
        using var context = _factory.CreateContext();
        var unitOfWork = new EfUnitOfWork(context);
        var users = new UserRepository(context);

        await using (await unitOfWork.BeginTransactionAsync(CancellationToken.None))
        {
            await users.AddAsync(User.Create("Ana", "ana@email.com", "hash", BaseTime), CancellationToken.None);
        }

        using var verifyContext = _factory.CreateContext();
        (await verifyContext.Users.CountAsync()).Should().Be(0);
    }
}
