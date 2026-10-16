using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Infrastructure.Common;
using Pomodoro.Infrastructure.Persistence;
using Pomodoro.Infrastructure.Repositories;
using Pomodoro.Infrastructure.Security;

namespace Pomodoro.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Variável de ambiente ConnectionStrings__Default (ou appsettings ConnectionStrings:Default) não configurada.");

        // `EnableRetryOnFailure`: sem isso, qualquer falha transitória de rede com o Postgres (blip de
        // conexão, banco ainda "acordando" num provedor free tier) derruba a requisição na primeira
        // tentativa em vez de tentar de novo — a causa mais provável de um 500/hang esporádico que não
        // se repete ao tentar de novo manualmente.
        services.AddDbContext<PomodoroDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPomodoroSessionRepository, PomodoroSessionRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IGuestImportRepository, GuestImportRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
