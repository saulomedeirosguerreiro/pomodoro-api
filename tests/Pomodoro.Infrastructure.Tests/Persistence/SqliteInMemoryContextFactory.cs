using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pomodoro.Infrastructure.Persistence;

namespace Pomodoro.Infrastructure.Tests.Persistence;

/// <summary>
/// Cria um banco SQLite em memória isolado por instância de teste (xUnit cria uma instância
/// nova da classe de teste por [Fact]/[Theory], então cada teste tem seu próprio banco).
/// </summary>
public sealed class SqliteInMemoryContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteInMemoryContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public PomodoroDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PomodoroDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new PomodoroDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
