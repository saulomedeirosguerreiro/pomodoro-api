using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pomodoro.Infrastructure.Persistence;

namespace Pomodoro.Api.Tests.Infrastructure;

/// <summary>
/// Sobe a Api inteira em memória (host real, pipeline real) contra um banco Postgres isolado por
/// instância da factory (container único por processo de teste, banco próprio por instância — ver
/// <see cref="PostgresTestDatabaseFactory"/>). Usada via <see cref="IClassFixture{TFixture}"/>.
/// </summary>
public sealed class PomodoroApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgresTestDatabaseFactory _dbFactory = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Satisfaz os checks de inicialização do Program.cs; a conexão real é trocada abaixo.
        builder.UseSetting("Jwt:Secret", "chave-de-teste-de-integracao-bem-grande-o-suficiente-32");
        builder.UseSetting("ConnectionStrings:Default", _dbFactory.ConnectionString);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PomodoroDbContext>>();
            services.AddDbContext<PomodoroDbContext>(options => options.UseNpgsql(_dbFactory.ConnectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _dbFactory.Dispose();
        }
    }
}
