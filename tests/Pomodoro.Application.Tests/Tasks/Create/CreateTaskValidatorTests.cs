using FluentAssertions;
using Pomodoro.Application.Tasks.Create;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Tasks.Create;

public class CreateTaskValidatorTests
{
    private readonly CreateTaskValidator _validator = new();

    [Fact]
    public void Validate_ComDadosValidos_NaoRetornaErros()
    {
        var request = new CreateTaskRequest("Relatório", "Fechar o mês", "media", 4);

        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ComTituloVazio_RetornaErroNoCampoTitle()
    {
        var request = new CreateTaskRequest("", null, "media", 4);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_ComTituloAcimaDoMaximo_RetornaErroNoCampoTitle()
    {
        var request = new CreateTaskRequest(new string('a', TaskItem.TitleMaxLength + 1), null, "media", 4);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_ComDescricaoAcimaDoMaximo_RetornaErroNoCampoDescription()
    {
        var request = new CreateTaskRequest("Relatório", new string('a', TaskItem.DescriptionMaxLength + 1), "media", 4);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Validate_ComPrioridadeInvalida_RetornaErroNoCampoPriority()
    {
        var request = new CreateTaskRequest("Relatório", null, "urgente", 4);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Priority");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Validate_ComEstimativaForaDoIntervalo_RetornaErroNoCampoEstimatedPomodoros(int estimate)
    {
        var request = new CreateTaskRequest("Relatório", null, "media", estimate);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EstimatedPomodoros");
    }
}
