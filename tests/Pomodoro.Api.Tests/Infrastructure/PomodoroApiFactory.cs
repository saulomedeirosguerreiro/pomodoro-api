using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pomodoro.Infrastructure.Persistence;

namespace Pomodoro.Api.Tests.Infrastructure;

/// <summary>
/// Sobe a Api inteira em memória (host real, pipeline real) contra um SQLite em memória
/// isolado por instância da factory. Usada via <see cref="IClassFixture{TFixture}"/>.
/// </summary>
public sealed class PomodoroApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PomodoroApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Satisfaz os checks de inicialização do Program.cs; a conexão real é trocada abaixo.
        builder.UseSetting("Jwt:Secret", "chave-de-teste-de-integracao-bem-grande-o-suficiente-32");
        builder.UseSetting("ConnectionStrings:Default", "DataSource=:memory:");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PomodoroDbContext>>();
            services.AddDbContext<PomodoroDbContext>(options => options.UseSqlite(_connection));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<PomodoroDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
