using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Pomodoro.Infrastructure.Repositories;
using Pomodoro.Infrastructure.Tests.Persistence;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryContextFactory _factory = new();
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AddAsync_PersisteEAtribuiId()
    {
        using var context = _factory.CreateContext();
        var repository = new UserRepository(context);
        var user = User.Create("João", "joao@email.com", "hash", UtcNow);

        await repository.AddAsync(user, CancellationToken.None);

        user.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExistsByEmailAsync_ComEmailEmOutraCaixa_RetornaTrue()
    {
        using (var seedContext = _factory.CreateContext())
        {
            await new UserRepository(seedContext).AddAsync(
                User.Create("João", "joao@email.com", "hash", UtcNow), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var exists = await new UserRepository(context).ExistsByEmailAsync("JOAO@EMAIL.COM", CancellationToken.None);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ComEmailNaoCadastrado_RetornaFalse()
    {
        using var context = _factory.CreateContext();
        var exists = await new UserRepository(context).ExistsByEmailAsync("ninguem@email.com", CancellationToken.None);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task FindByEmailAsync_ComEmailCadastrado_RetornaUsuario()
    {
        using (var seedContext = _factory.CreateContext())
        {
            await new UserRepository(seedContext).AddAsync(
                User.Create("João", "joao@email.com", "hash", UtcNow), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var user = await new UserRepository(context).FindByEmailAsync("joao@email.com", CancellationToken.None);

        user.Should().NotBeNull();
        user!.Name.Should().Be("João");
    }

    [Fact]
    public async Task FindByEmailAsync_ComEmailNaoCadastrado_RetornaNull()
    {
        using var context = _factory.CreateContext();
        var user = await new UserRepository(context).FindByEmailAsync("ninguem@email.com", CancellationToken.None);

        user.Should().BeNull();
    }

    [Fact]
    public async Task FindByIdAsync_ComIdExistente_RetornaUsuario()
    {
        int id;
        using (var seedContext = _factory.CreateContext())
        {
            var user = User.Create("João", "joao@email.com", "hash", UtcNow);
            await new UserRepository(seedContext).AddAsync(user, CancellationToken.None);
            id = user.Id;
        }

        using var context = _factory.CreateContext();
        var found = await new UserRepository(context).FindByIdAsync(id, CancellationToken.None);

        found.Should().NotBeNull();
    }

    [Fact]
    public async Task FindByIdAsync_ComIdInexistente_RetornaNull()
    {
        using var context = _factory.CreateContext();
        var found = await new UserRepository(context).FindByIdAsync(999, CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task Indice_UnicoDeEmail_ImpedeEmailDuplicadoIndependenteDaCaixa()
    {
        using (var seedContext = _factory.CreateContext())
        {
            await new UserRepository(seedContext).AddAsync(
                User.Create("João", "joao@email.com", "hash", UtcNow), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var duplicate = User.Create("Outro João", "JOAO@EMAIL.COM", "hash2", UtcNow);

        var act = () => new UserRepository(context).AddAsync(duplicate, CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task DeleteAsync_RemoveOUsuario()
    {
        int id;
        using (var seedContext = _factory.CreateContext())
        {
            var user = User.Create("João", "joao@email.com", "hash", UtcNow);
            await new UserRepository(seedContext).AddAsync(user, CancellationToken.None);
            id = user.Id;
        }

        using (var context = _factory.CreateContext())
        {
            var repository = new UserRepository(context);
            var user = await repository.FindByIdAsync(id, CancellationToken.None);
            await repository.DeleteAsync(user!, CancellationToken.None);
        }

        using var assertContext = _factory.CreateContext();
        var found = await new UserRepository(assertContext).FindByIdAsync(id, CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemoveEmCascataSessoesTarefasEConquistas()
    {
        int userId;
        using (var seedContext = _factory.CreateContext())
        {
            var user = User.Create("João", "joao@email.com", "hash", UtcNow);
            await new UserRepository(seedContext).AddAsync(user, CancellationToken.None);
            userId = user.Id;

            var session = PomodoroSession.Create(
                userId, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
                UtcNow, UtcNow.AddSeconds(SessionTypeDurations.FocoSeconds), UtcNow);
            seedContext.PomodoroSessions.Add(session);

            var task = TaskItem.Create(userId, "Tarefa", null, TaskPriority.Media, 1, UtcNow);
            seedContext.Tasks.Add(task);

            seedContext.UserAchievements.Add(UserAchievement.Create(userId, "primeira_semente", UtcNow));

            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        using (var context = _factory.CreateContext())
        {
            var repository = new UserRepository(context);
            var user = await repository.FindByIdAsync(userId, CancellationToken.None);
            await repository.DeleteAsync(user!, CancellationToken.None);
        }

        using var assertContext = _factory.CreateContext();
        (await assertContext.PomodoroSessions.AnyAsync(s => s.UserId == userId)).Should().BeFalse();
        (await assertContext.Tasks.AnyAsync(t => t.UserId == userId)).Should().BeFalse();
        (await assertContext.UserAchievements.AnyAsync(a => a.UserId == userId)).Should().BeFalse();
    }
}
