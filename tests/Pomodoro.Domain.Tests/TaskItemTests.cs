using FluentAssertions;
using Pomodoro.Domain.Common;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Domain.Tests;

public class TaskItemTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ComDadosValidos_PreencheTodosOsCampos()
    {
        var task = TaskItem.Create(1, " Revisar PR ", " Descrição ", TaskPriority.Alta, 4, UtcNow);

        task.UserId.Should().Be(1);
        task.Title.Should().Be("Revisar PR");
        task.Description.Should().Be("Descrição");
        task.Priority.Should().Be(TaskPriority.Alta);
        task.EstimatedPomodoros.Should().Be(4);
        task.Status.Should().Be(TaskItemStatus.AFazer);
        task.CreatedAt.Should().Be(UtcNow);
        task.UpdatedAt.Should().Be(UtcNow);
    }

    [Fact]
    public void Create_SemDescricao_AceitaNulo()
    {
        var task = TaskItem.Create(1, "Tarefa", null, TaskPriority.Baixa, 1, UtcNow);

        task.Description.Should().BeNull();
    }

    [Fact]
    public void Create_ComDescricaoEmBranco_ViraNulo()
    {
        var task = TaskItem.Create(1, "Tarefa", "   ", TaskPriority.Baixa, 1, UtcNow);

        task.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ComUserIdInvalido_LancaDomainException(int userId)
    {
        var act = () => TaskItem.Create(userId, "Tarefa", null, TaskPriority.Media, 1, UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ComTituloInvalido_LancaDomainException(string title)
    {
        var act = () => TaskItem.Create(1, title, null, TaskPriority.Media, 1, UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ComTituloAcimaDoLimite_LancaDomainException()
    {
        var longTitle = new string('a', TaskItem.TitleMaxLength + 1);

        var act = () => TaskItem.Create(1, longTitle, null, TaskPriority.Media, 1, UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ComDescricaoAcimaDoLimite_LancaDomainException()
    {
        var longDescription = new string('a', TaskItem.DescriptionMaxLength + 1);

        var act = () => TaskItem.Create(1, "Tarefa", longDescription, TaskPriority.Media, 1, UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Create_ComEstimativaForaDoIntervalo_LancaDomainException(int estimate)
    {
        var act = () => TaskItem.Create(1, "Tarefa", null, TaskPriority.Media, estimate, UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateDetails_AtualizaCamposEUpdatedAt()
    {
        var task = TaskItem.Create(1, "Original", null, TaskPriority.Baixa, 1, UtcNow);
        var later = UtcNow.AddDays(1);

        task.UpdateDetails("Novo título", "Nova descrição", TaskPriority.Alta, 3, later);

        task.Title.Should().Be("Novo título");
        task.Description.Should().Be("Nova descrição");
        task.Priority.Should().Be(TaskPriority.Alta);
        task.EstimatedPomodoros.Should().Be(3);
        task.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void MarkAsFocused_ColocaEmCurso()
    {
        var task = TaskItem.Create(1, "Tarefa", null, TaskPriority.Media, 1, UtcNow);

        task.MarkAsFocused(UtcNow.AddHours(1));

        task.Status.Should().Be(TaskItemStatus.EmCurso);
    }

    [Fact]
    public void MarkAsFocused_ComTarefaJaFeita_LancaDomainException()
    {
        var task = TaskItem.Create(1, "Tarefa", null, TaskPriority.Media, 1, UtcNow);
        task.MarkAsDone(UtcNow);

        var act = () => task.MarkAsFocused(UtcNow.AddHours(1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsTodo_VoltaParaAFazer()
    {
        var task = TaskItem.Create(1, "Tarefa", null, TaskPriority.Media, 1, UtcNow);
        task.MarkAsFocused(UtcNow);

        task.MarkAsTodo(UtcNow.AddHours(1));

        task.Status.Should().Be(TaskItemStatus.AFazer);
    }

    [Fact]
    public void MarkAsDone_MarcaComoFeito()
    {
        var task = TaskItem.Create(1, "Tarefa", null, TaskPriority.Media, 1, UtcNow);

        task.MarkAsDone(UtcNow.AddHours(1));

        task.Status.Should().Be(TaskItemStatus.Feito);
    }
}
