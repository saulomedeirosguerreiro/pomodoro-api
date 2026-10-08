using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Pomodoro.Api.Tests;

public class StartupTests
{

    [Fact]
    public void Host_SemJwtSecretConfigurado_FalhaAoSubir()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Jwt:Secret", "");
            builder.UseSetting("ConnectionStrings:Default", "DataSource=:memory:");
        });

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT_SECRET*");
    }

    [Fact]
    public void Host_ComJwtSecretApenasViaChaveFlatJWT_SECRET_SobeNormalmente()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("JWT_SECRET", "chave-via-variavel-flat-bem-grande-o-suficiente-32");
            builder.UseSetting("ConnectionStrings:Default", "DataSource=:memory:");
        });

        var act = () => factory.CreateClient();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task Host_ComCorsOrigemViaChaveFlatCORS_ORIGIN_LiberaAOrigemConfigurada()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Jwt:Secret", "chave-de-teste-bem-grande-o-suficiente-32-caracteres");
            builder.UseSetting("ConnectionStrings:Default", "DataSource=:memory:");
            // appsettings.Development.json já define Cors:AllowedOrigin; zera para forçar o fallback CORS_ORIGIN.
            builder.UseSetting("Cors:AllowedOrigin", "");
            builder.UseSetting("CORS_ORIGIN", "http://frontend-teste.local");
        });
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("Origin", "http://frontend-teste.local");
        var response = await client.SendAsync(request);

        response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin");
    }
}
