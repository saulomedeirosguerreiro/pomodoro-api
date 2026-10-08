using FluentAssertions;
using Pomodoro.Domain.Common;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Domain.Tests;

public class UserAchievementTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ComDadosValidos_PreencheTodosOsCampos()
    {
        var achievement = UserAchievement.Create(1, "primeira_semente", UtcNow);

        achievement.UserId.Should().Be(1);
        achievement.Code.Should().Be("primeira_semente");
        achievement.UnlockedAt.Should().Be(UtcNow);
    }

    [Fact]
    public void Create_ComUserIdInvalido_LancaDomainException()
    {
        var act = () => UserAchievement.Create(0, "primeira_semente", UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ComCodigoInvalido_LancaDomainException(string? code)
    {
        var act = () => UserAchievement.Create(1, code!, UtcNow);

        act.Should().Throw<DomainException>();
    }
}
