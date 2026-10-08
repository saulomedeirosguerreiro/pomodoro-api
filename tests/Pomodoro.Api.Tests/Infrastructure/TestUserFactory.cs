using System.Net.Http.Json;

namespace Pomodoro.Api.Tests.Infrastructure;

public static class TestUserFactory
{
    public sealed record RegisteredUser(string Email, string Password, string Token);

    /// <summary>Cadastra e loga um usuário com e-mail único, retornando o token pronto para uso.</summary>
    public static async Task<RegisteredUser> RegisterAndLoginAsync(HttpClient client, string? emailPrefix = null)
    {
        var email = $"{emailPrefix ?? "user"}-{Guid.NewGuid():N}@email.com";
        const string password = "Senha123";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Usuário de Teste",
            email,
            password
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponseBody>();
        return new RegisteredUser(email, password, body!.Token);
    }

    private sealed record LoginResponseBody(string Token);
}
