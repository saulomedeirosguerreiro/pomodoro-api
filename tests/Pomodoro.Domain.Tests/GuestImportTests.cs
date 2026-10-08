using FluentAssertions;
using Pomodoro.Domain.Common;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Domain.Tests;

public class GuestImportTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ComDadosValidos_PreencheTodosOsCampos()
    {
        var import = GuestImport.Create(
            1, " guest-123 ", UtcNow, tasksImported: 3, sessionsImported: 5,
            achievementsUnlockedJson: "[\"primeira_semente\"]", skippedItemsJson: "[]");

        import.UserId.Should().Be(1);
        import.GuestId.Should().Be("guest-123");
        import.ImportedAt.Should().Be(UtcNow);
        import.TasksImported.Should().Be(3);
        import.SessionsImported.Should().Be(5);
        import.AchievementsUnlockedJson.Should().Be("[\"primeira_semente\"]");
        import.SkippedItemsJson.Should().Be("[]");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ComUserIdInvalido_LancaDomainException(int userId)
    {
        var act = () => GuestImport.Create(userId, "guest-123", UtcNow, 0, 0, "[]", "[]");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComGuestIdInvalido_LancaDomainException(string? guestId)
    {
        var act = () => GuestImport.Create(1, guestId!, UtcNow, 0, 0, "[]", "[]");

        act.Should().Throw<DomainException>();
    }
}
