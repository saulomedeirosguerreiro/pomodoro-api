using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Pomodoro.Api.Common;
using Pomodoro.Api.Endpoints;
using Pomodoro.Api.Seed;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Achievements;
using Pomodoro.Application.Achievements.Evaluate;
using Pomodoro.Application.Achievements.List;
using Pomodoro.Application.Auth.Login;
using Pomodoro.Application.Auth.Register;
using Pomodoro.Application.Pomodoros.Create;
using Pomodoro.Application.Pomodoros.GetById;
using Pomodoro.Application.Pomodoros.List;
using Pomodoro.Application.Progress.GetProgress;
using Pomodoro.Application.Tasks.Create;
using Pomodoro.Application.Tasks.Delete;
using Pomodoro.Application.Tasks.List;
using Pomodoro.Application.Tasks.SetStatus;
using Pomodoro.Application.Tasks.Update;
using Pomodoro.Application.Users.Delete;
using Pomodoro.Application.Users.GetProfile;
using Pomodoro.Infrastructure.DependencyInjection;

// Preserva o nome original das claims ("sub", "email") em vez do mapeamento legado
// para URNs de ClaimTypes — sem isso, user.FindFirstValue(JwtRegisteredClaimNames.Sub) nunca encontra nada.
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

// RNF-02 / US-21 CA-001: a Api nunca sobe sem um segredo de JWT configurado.
var jwtSecret = FirstNonBlank(builder.Configuration["Jwt:Secret"], builder.Configuration["JWT_SECRET"]);
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT_SECRET (ou Jwt:Secret) não configurado. Defina a variável de ambiente antes de subir a Api.");
}

// `JWT_SECRET` é uma chave "solta" (sem o prefixo `Jwt:`), então o provider de env vars do
// ASP.NET Core nunca a mapeia para a seção `Jwt` usada por JwtOptions/JwtTokenService — sem esta
// linha, a Api sobe (o guard acima só olha as duas fontes isoladamente) mas o login quebra com
// "key length is zero" assim que tenta assinar um token.
builder.Configuration["Jwt:Secret"] = jwtSecret;

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();

builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<GetUserProfileHandler>();
builder.Services.AddScoped<DeleteAccountHandler>();
builder.Services.AddScoped<CreatePomodoroHandler>();
builder.Services.AddScoped<ListPomodorosHandler>();
builder.Services.AddScoped<GetPomodoroByIdHandler>();
builder.Services.AddScoped<GetProgressHandler>();
builder.Services.AddScoped<CreateTaskHandler>();
builder.Services.AddScoped<ListTasksHandler>();
builder.Services.AddScoped<UpdateTaskHandler>();
builder.Services.AddScoped<SetTaskStatusHandler>();
builder.Services.AddScoped<DeleteTaskHandler>();
builder.Services.AddScoped<AchievementStatsProvider>();
builder.Services.AddScoped<IAchievementEvaluator, EvaluateAchievementsHandler>();
builder.Services.AddScoped<ListAchievementsHandler>();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// US-21 RN-02: CORS liberado só para a origem configurada do frontend.
var allowedOrigin = FirstNonBlank(builder.Configuration["Cors:AllowedOrigin"], builder.Configuration["CORS_ORIGIN"]);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (!string.IsNullOrWhiteSpace(allowedOrigin))
        {
            policy.WithOrigins(allowedOrigin).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// `dotnet run -- seed` cria o usuário de teste (US-25) e encerra, sem subir o servidor.
if (args.Contains("seed"))
{
    await SeedData.RunAsync(app.Services);
    return;
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUsersEndpoints();
app.MapPomodorosEndpoints();
app.MapProgressEndpoints();
app.MapTasksEndpoints();
app.MapAchievementsEndpoints();

app.Run();

/// <summary>Exposta para <c>WebApplicationFactory&lt;Program&gt;</c> nos testes de integração.</summary>
public partial class Program
{
    /// <summary>
    /// Primeiro valor não vazio/não branco entre as fontes de configuração, na ordem dada.
    /// Diferente de `??`, trata string vazia (ex.: default `""` em appsettings.json) como ausente.
    /// </summary>
    public static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
