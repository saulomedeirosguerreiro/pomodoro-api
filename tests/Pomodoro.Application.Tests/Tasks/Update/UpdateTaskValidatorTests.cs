using FluentAssertions;
using Pomodoro.Application.Tasks.Update;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Tasks.Update;

public class UpdateTaskValidatorTests
{
    private readonly UpdateTaskValidator _validator = new();

    [Fact]
    public void Validate_ComDadosValidos_NaoRetornaErros()
    {
        var request = new UpdateTaskRequest("Relatório", "Fechar o mês", "alta", 6);

        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ComTituloVazio_RetornaErroNoCampoTitle()
    {
        var request = new UpdateTaskRequest("", null, "media", 4);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_ComPrioridadeInvalida_RetornaErroNoCampoPriority()
    {
        var request = new UpdateTaskRequest("Relatório", null, "urgente", 4);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Priority");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Validate_ComEstimativaForaDoIntervalo_RetornaErroNoCampoEstimatedPomodoros(int estimate)
    {
        var request = new UpdateTaskRequest("Relatório", null, "media", estimate);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EstimatedPomodoros");
    }

    [Fact]
    public void Validate_ComDescricaoAcimaDoMaximo_RetornaErroNoCampoDescription()
    {
        var request = new UpdateTaskRequest("Relatório", new string('a', TaskItem.DescriptionMaxLength + 1), "media", 4);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}
