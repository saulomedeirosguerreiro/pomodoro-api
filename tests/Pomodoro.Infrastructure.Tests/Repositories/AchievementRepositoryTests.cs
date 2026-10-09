using FluentAssertions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Repositories;
using Pomodoro.Infrastructure.Tests.Persistence;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Repositories;

public class AchievementRepositoryTests : IDisposable
{
    private readonly PostgresTestDatabaseFactory _factory = new();
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public void Dispose() => _factory.Dispose();

    private async Task SeedUsersAsync(int count)
    {
        using var context = _factory.CreateContext();
        var repository = new UserRepository(context);
        for (var i = 0; i < count; i++)
        {
            await repository.AddAsync(
                User.Create($"Usuário {i + 1}", $"user{i + 1}@email.com", "hash", BaseTime), CancellationToken.None);
        }
    }

    [Fact]
    public async Task AddAsync_PersisteEAtribuiId()
    {
        await SeedUsersAsync(1);
        using var context = _factory.CreateContext();
        var repository = new AchievementRepository(context);
        var achievement = UserAchievement.Create(1, "primeira_semente", BaseTime);

        await repository.AddAsync(achievement, CancellationToken.None);

        achievement.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListForUserAsync_RetornaApenasAsConquistasDoUsuario()
    {
        await SeedUsersAsync(2);
        using (var seedContext = _factory.CreateContext())
        {
            var repo = new AchievementRepository(seedContext);
            await repo.AddAsync(UserAchievement.Create(1, "primeira_semente", BaseTime), CancellationToken.None);
            await repo.AddAsync(UserAchievement.Create(1, "cem_tomates", BaseTime.AddDays(1)), CancellationToken.None);
            await repo.AddAsync(UserAchievement.Create(2, "primeira_semente", BaseTime), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var items = await new AchievementRepository(context).ListForUserAsync(1, CancellationToken.None);

        items.Should().HaveCount(2);
        items.Should().OnlyContain(a => a.UserId == 1);
    }

    [Fact]
    public void IsUniqueConstraintViolation_ComInnerExceptionQueNaoEDoPostgres_RetornaFalse()
    {
        var exception = new Microsoft.EntityFrameworkCore.DbUpdateException(
            "falha genérica", new InvalidOperationException("não é do Postgres"));

        AchievementRepository.IsUniqueConstraintViolation(exception).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ComUserIdInexistente_LancaDbUpdateExceptionBruta_NaoTraduzParaDuplicateUserAchievementException()
    {
        using var context = _factory.CreateContext();
        var repository = new AchievementRepository(context);
        var achievement = UserAchievement.Create(999, "primeira_semente", BaseTime);

        var act = () => repository.AddAsync(achievement, CancellationToken.None);

        await act.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>();
    }

    [Fact]
    public async Task Indice_UnicoDeUserIdECodigo_ImpedeDuplicado_LancaDuplicateUserAchievementException()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            await new AchievementRepository(seedContext).AddAsync(
                UserAchievement.Create(1, "primeira_semente", BaseTime), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var duplicate = UserAchievement.Create(1, "primeira_semente", BaseTime);

        var act = () => new AchievementRepository(context).AddAsync(duplicate, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateUserAchievementException>();
    }

    [Fact]
    public async Task ListForUserAsync_SemConquistas_RetornaListaVazia()
    {
        await SeedUsersAsync(1);
        using var context = _factory.CreateContext();

        var items = await new AchievementRepository(context).ListForUserAsync(1, CancellationToken.None);

        items.Should().BeEmpty();
    }
}
