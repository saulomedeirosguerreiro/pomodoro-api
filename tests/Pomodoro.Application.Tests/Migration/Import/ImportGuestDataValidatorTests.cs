using FluentAssertions;
using Pomodoro.Application.Migration.Import;
using Xunit;

namespace Pomodoro.Application.Tests.Migration.Import;

public class ImportGuestDataValidatorTests
{
    private readonly ImportGuestDataValidator _validator = new();

    private static ImportGuestDataRequest ValidRequest() => new(
        "guest-123", Array.Empty<GuestTaskImportItem>(), Array.Empty<GuestSessionImportItem>());

    [Fact]
    public void Validate_ComDadosValidos_NaoRetornaErros()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ComGuestIdVazio_RetornaErroNoCampoGuestId(string guestId)
    {
        var result = _validator.Validate(ValidRequest() with { GuestId = guestId });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GuestId");
    }

    [Fact]
    public void Validate_ComGuestIdAcimaDoLimite_RetornaErroNoCampoGuestId()
    {
        var result = _validator.Validate(ValidRequest() with { GuestId = new string('a', 101) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GuestId");
    }

    [Fact]
    public void Validate_ComLoteDeTarefasAcimaDoLimite_RetornaErroNoCampoTasks()
    {
        var tasks = Enumerable.Range(0, ImportGuestDataValidator.MaxTasks + 1)
            .Select(i => new GuestTaskImportItem(
                $"task-{i}", "Título", null, "media", 1, "a_fazer", null, null))
            .ToList();

        var result = _validator.Validate(ValidRequest() with { Tasks = tasks });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tasks");
    }

    [Fact]
    public void Validate_ComLoteDeSessoesAcimaDoLimite_RetornaErroNoCampoSessions()
    {
        var sessions = Enumerable.Range(0, ImportGuestDataValidator.MaxSessions + 1)
            .Select(i => new GuestSessionImportItem(
                $"session-{i}", "foco", "concluido", 1500, DateTime.UtcNow, DateTime.UtcNow, null))
            .ToList();

        var result = _validator.Validate(ValidRequest() with { Sessions = sessions });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Sessions");
    }
}
