using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pomodoro.Infrastructure.Persistence;
using Xunit;

namespace Pomodoro.Api.Tests;

/// <summary>
/// Regressão: o README documenta `export JWT_SECRET=...` (chave "solta", sem o prefixo `Jwt:`) como a
/// forma oficial de configurar o segredo. O provider de env vars do ASP.NET Core nunca mapeia essa chave
/// para a seção `Jwt` usada por JwtOptions/JwtTokenService, então sem o "replay" feito em Program.cs
/// (`builder.Configuration["Jwt:Secret"] = jwtSecret`) a Api sobe normalmente mas o login quebra com
/// "key length is zero" ao tentar assinar o token. Este teste configura a Api do mesmo jeito que o
/// usuário faria seguindo o README — só a chave plana — para travar esse comportamento.
/// </summary>
public sealed class JwtSecretFromFlatEnvVarTests : IClassFixture<JwtSecretFromFlatEnvVarTests.FlatEnvVarFactory>
{
    private readonly FlatEnvVarFactory _factory;

    public JwtSecretFromFlatEnvVarTests(FlatEnvVarFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ComSegredoConfiguradoSoViaJWT_SECRET_GeraTokenQueAutenticaChamadasPosteriores()
    {
        var client = _factory.CreateClient();
        const string email = "jwtflatenv@email.com";
        await client.PostAsJsonAsync("/api/auth/register", new { name = "Flat Env", email, password = "Senha123" });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Senha123" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginBody>();
        body!.Token.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        var meResponse = await client.GetAsync("/api/users/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record LoginBody(string Token);

    public sealed class FlatEnvVarFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        public FlatEnvVarFactory()
        {
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // De propósito: só a chave plana "JWT_SECRET", exatamente como `export JWT_SECRET=...` do README
            // produziria via EnvironmentVariablesConfigurationProvider — nunca "Jwt:Secret" diretamente.
            builder.UseSetting("JWT_SECRET", "chave-de-teste-via-env-var-plana-32-caracteres-ok");
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
}
