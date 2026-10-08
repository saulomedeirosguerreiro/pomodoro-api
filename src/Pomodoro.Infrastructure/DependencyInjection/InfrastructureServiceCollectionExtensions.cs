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

        services.AddDbContext<PomodoroDbContext>(options => options.UseSqlite(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPomodoroSessionRepository, PomodoroSessionRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IGuestImportRepository, GuestImportRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
