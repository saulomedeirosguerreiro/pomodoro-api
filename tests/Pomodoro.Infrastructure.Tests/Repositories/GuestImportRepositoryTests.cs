using FluentAssertions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Repositories;
using Pomodoro.Infrastructure.Tests.Persistence;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Repositories;

public class GuestImportRepositoryTests : IDisposable
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
        var repository = new GuestImportRepository(context);
        var import = GuestImport.Create(1, "guest-1", BaseTime, 2, 3, "[]", "[]");

        await repository.AddAsync(import, CancellationToken.None);

        import.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FindAsync_ComUserIdEGuestIdCadastrados_RetornaORegistro()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            await new GuestImportRepository(seedContext).AddAsync(
                GuestImport.Create(1, "guest-1", BaseTime, 2, 3, "[]", "[]"), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var found = await new GuestImportRepository(context).FindAsync(1, "guest-1", CancellationToken.None);

        found.Should().NotBeNull();
        found!.TasksImported.Should().Be(2);
    }

    [Fact]
    public async Task FindAsync_ComGuestIdDeOutroUsuario_RetornaNull()
    {
        await SeedUsersAsync(2);
        using (var seedContext = _factory.CreateContext())
        {
            await new GuestImportRepository(seedContext).AddAsync(
                GuestImport.Create(1, "guest-1", BaseTime, 2, 3, "[]", "[]"), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var found = await new GuestImportRepository(context).FindAsync(2, "guest-1", CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_SemRegistro_RetornaNull()
    {
        await SeedUsersAsync(1);
        using var context = _factory.CreateContext();

        var found = await new GuestImportRepository(context).FindAsync(1, "guest-inexistente", CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public void IsUniqueConstraintViolation_ComInnerExceptionQueNaoEDoPostgres_RetornaFalse()
    {
        var exception = new Microsoft.EntityFrameworkCore.DbUpdateException(
            "falha genérica", new InvalidOperationException("não é do Postgres"));

        GuestImportRepository.IsUniqueConstraintViolation(exception).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ComUserIdInexistente_LancaDbUpdateExceptionBruta_NaoTraduzParaDuplicateGuestImportException()
    {
        using var context = _factory.CreateContext();
        var repository = new GuestImportRepository(context);
        var import = GuestImport.Create(999, "guest-orfao", BaseTime, 0, 0, "[]", "[]");

        var act = () => repository.AddAsync(import, CancellationToken.None);

        await act.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>();
    }

    [Fact]
    public async Task Indice_UnicoDeUserIdEGuestId_ImpedeDuplicado_LancaDuplicateGuestImportException()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            await new GuestImportRepository(seedContext).AddAsync(
                GuestImport.Create(1, "guest-1", BaseTime, 2, 3, "[]", "[]"), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var duplicate = GuestImport.Create(1, "guest-1", BaseTime, 9, 9, "[]", "[]");

        var act = () => new GuestImportRepository(context).AddAsync(duplicate, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateGuestImportException>();
    }
}
