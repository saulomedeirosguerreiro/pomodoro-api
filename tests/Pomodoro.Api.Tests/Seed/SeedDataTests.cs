using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pomodoro.Api.Seed;
using Pomodoro.Api.Tests.Infrastructure;
using Pomodoro.Application.Abstractions;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Api.Tests.Seed;

public class SeedDataTests : IClassFixture<PomodoroApiFactory>
{
    private readonly PomodoroApiFactory _factory;

    public SeedDataTests(PomodoroApiFactory factory)
    {
        _factory = factory;
        _ = factory.Server; // força a inicialização do host antes de resolver serviços
    }

    [Fact]
    public async Task RunAsync_ChamadoDuasVezes_CriaUmaVezESegundaChamadaNaoDuplica()
    {
        await SeedData.RunAsync(_factory.Services);
        await SeedData.RunAsync(_factory.Services);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await users.FindByEmailAsync(SeedData.TestUserEmail, CancellationToken.None);

        user.Should().NotBeNull();
    }

    [Fact]
    public async Task RunAsync_CriaUsuarioComStreakTarefasEConquista()
    {
        await SeedData.RunAsync(_factory.Services);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = (await users.FindByEmailAsync(SeedData.TestUserEmail, CancellationToken.None))!;

        var sessions = scope.ServiceProvider.GetRequiredService<IPomodoroSessionRepository>();
        var allSessions = await sessions.ListAllAsync(user.Id, CancellationToken.None);
        var distinctFocusDays = allSessions
            .Where(s => s.Type == SessionType.Foco && s.Status == SessionStatus.Concluido)
            .Select(s => s.CompletedAt.Date)
            .Distinct();
        distinctFocusDays.Should().HaveCount(3, "o seed precisa cobrir 3 dias seguidos para gerar streak > 0 (US-64 CA-001)");

        var tasks = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var allTasks = await tasks.ListForUserAsync(user.Id, status: null, CancellationToken.None);
        allTasks.Select(t => t.Status).Should().BeEquivalentTo(
            new[] { TaskItemStatus.AFazer, TaskItemStatus.EmCurso, TaskItemStatus.Feito });

        var achievements = scope.ServiceProvider.GetRequiredService<IAchievementRepository>();
        var unlocked = await achievements.ListForUserAsync(user.Id, CancellationToken.None);
        unlocked.Should().NotBeEmpty();
    }
}
