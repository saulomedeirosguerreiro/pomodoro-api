using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pomodoro.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Pomodoro.Infrastructure.Tests.Persistence;

/// <summary>
/// Container Postgres único por processo de teste (iniciado de forma preguiçosa, na primeira
/// instância criada). Cada <c>new PostgresTestDatabaseFactory()</c> (uma por teste, mesma forma de
/// uso de <c>SqliteInMemoryContextFactory</c>) cria um banco próprio nesse container e aplica o
/// schema real via <see cref="DatabaseFacade.Migrate"/> — não <c>EnsureCreated()</c> — validando a
/// migration gerada (extensão citext, identity, ordem de criação) contra um Postgres de verdade.
/// </summary>
public sealed class PostgresTestDatabaseFactory : IDisposable
{
    private static readonly PostgreSqlContainer Container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private static readonly SemaphoreSlim StartLock = new(1, 1);
    private static bool _started;

    private readonly string _databaseName = $"test_{Guid.NewGuid():N}";
    private readonly string _connectionString;

    public PostgresTestDatabaseFactory()
    {
        EnsureContainerStartedAsync().GetAwaiter().GetResult();

        var adminConnectionString = Container.GetConnectionString();
        using (var adminConnection = new NpgsqlConnection(adminConnectionString))
        {
            adminConnection.Open();
            using var createCommand = adminConnection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            createCommand.ExecuteNonQuery();
        }

        // Pooling=false: sem isso, conexões do pool ficam presas ao banco e o DROP DATABASE no
        // Dispose() trava esperando elas fecharem.
        _connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = _databaseName,
            Pooling = false,
        }.ConnectionString;

        using var context = CreateContext();
        context.Database.Migrate();
    }

    public PomodoroDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PomodoroDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new PomodoroDbContext(options);
    }

    public void Dispose()
    {
        var adminConnectionString = Container.GetConnectionString();
        using var adminConnection = new NpgsqlConnection(adminConnectionString);
        adminConnection.Open();
        using var dropCommand = adminConnection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
        dropCommand.ExecuteNonQuery();
    }

    private static async Task EnsureContainerStartedAsync()
    {
        if (_started)
        {
            return;
        }

        await StartLock.WaitAsync();
        try
        {
            if (!_started)
            {
                await Container.StartAsync();
                _started = true;
            }
        }
        finally
        {
            StartLock.Release();
        }
    }
}
