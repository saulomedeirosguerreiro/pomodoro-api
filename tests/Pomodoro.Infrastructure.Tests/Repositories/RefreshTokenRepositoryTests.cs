using FluentAssertions;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Repositories;
using Pomodoro.Infrastructure.Tests.Persistence;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Repositories;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly PostgresTestDatabaseFactory _factory = new();
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Dispose() => _factory.Dispose();

    private async Task<int> SeedUserAsync()
    {
        using var context = _factory.CreateContext();
        var user = User.Create("João", $"joao-{Guid.NewGuid():N}@email.com", "hash", UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);
        return user.Id;
    }

    [Fact]
    public async Task AddAsync_PersisteOToken()
    {
        var userId = await SeedUserAsync();
        using var context = _factory.CreateContext();
        var repository = new RefreshTokenRepository(context);

        await repository.AddAsync(RefreshToken.Create(userId, "hash-1", UtcNow, UtcNow.AddDays(30)), CancellationToken.None);

        var found = await repository.FindByHashAsync("hash-1", CancellationToken.None);
        found.Should().NotBeNull();
        found!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task FindByHashAsync_ComHashInexistente_RetornaNull()
    {
        using var context = _factory.CreateContext();
        var found = await new RefreshTokenRepository(context).FindByHashAsync("nao-existe", CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_RevogaSoOsTokensAtivosDoUsuario()
    {
        var userId = await SeedUserAsync();
        var outroUserId = await SeedUserAsync();

        using (var seedContext = _factory.CreateContext())
        {
            var seedRepository = new RefreshTokenRepository(seedContext);
            await seedRepository.AddAsync(RefreshToken.Create(userId, "hash-ativo-1", UtcNow, UtcNow.AddDays(30)), CancellationToken.None);
            await seedRepository.AddAsync(RefreshToken.Create(userId, "hash-ativo-2", UtcNow, UtcNow.AddDays(30)), CancellationToken.None);
            await seedRepository.AddAsync(RefreshToken.Create(outroUserId, "hash-outro-usuario", UtcNow, UtcNow.AddDays(30)), CancellationToken.None);
        }

        using (var context = _factory.CreateContext())
        {
            await new RefreshTokenRepository(context).RevokeAllForUserAsync(userId, UtcNow.AddDays(1), CancellationToken.None);
        }

        using var assertContext = _factory.CreateContext();
        var assertRepository = new RefreshTokenRepository(assertContext);
        (await assertRepository.FindByHashAsync("hash-ativo-1", CancellationToken.None))!.RevokedAt.Should().Be(UtcNow.AddDays(1));
        (await assertRepository.FindByHashAsync("hash-ativo-2", CancellationToken.None))!.RevokedAt.Should().Be(UtcNow.AddDays(1));
        (await assertRepository.FindByHashAsync("hash-outro-usuario", CancellationToken.None))!.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task IndiceUnicoDeTokenHash_ImpedeHashDuplicado()
    {
        var userId = await SeedUserAsync();
        using (var seedContext = _factory.CreateContext())
        {
            await new RefreshTokenRepository(seedContext).AddAsync(
                RefreshToken.Create(userId, "hash-duplicado", UtcNow, UtcNow.AddDays(30)), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var act = () => new RefreshTokenRepository(context).AddAsync(
            RefreshToken.Create(userId, "hash-duplicado", UtcNow, UtcNow.AddDays(30)), CancellationToken.None);

        await act.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>();
    }
}
