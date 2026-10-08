using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Infrastructure.DependencyInjection;
using Pomodoro.Infrastructure.Persistence;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.DependencyInjection;

public class InfrastructureServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfiguration(string? connectionString = "DataSource=:memory:") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Jwt:Secret"] = "uma-chave-secreta-de-teste-bem-grande-32+"
            })
            .Build();

    [Fact]
    public void AddInfrastructure_ComConnectionStringConfigurada_RegistraTodasAsDependencias()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(BuildConfiguration());
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<PomodoroDbContext>().Should().NotBeNull();
        provider.GetRequiredService<IUserRepository>().Should().NotBeNull();
        provider.GetRequiredService<IPomodoroSessionRepository>().Should().NotBeNull();
        provider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        provider.GetRequiredService<IPasswordHasher>().Should().NotBeNull();
        provider.GetRequiredService<ITokenService>().Should().NotBeNull();
        provider.GetRequiredService<IDateTimeProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_SemConnectionString_LancaInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: null);

        var act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>();
    }
}
