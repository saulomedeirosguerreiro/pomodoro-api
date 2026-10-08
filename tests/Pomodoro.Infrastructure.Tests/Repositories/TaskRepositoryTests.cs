using FluentAssertions;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Pomodoro.Infrastructure.Repositories;
using Pomodoro.Infrastructure.Tests.Persistence;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Repositories;

public class TaskRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryContextFactory _factory = new();
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public void Dispose() => _factory.Dispose();

    private async Task SeedUsersAsync(int count)
    {
        using var context = _factory.CreateContext();
        var repository = new UserRepository(context);
        for (var i = 0; i < count; i++)
        {
            await repository.AddAsync(
                User.Create($"Usuário {i + 1}", $"user{i + 1}@email.com", "hash", BaseTime), CancellationToken.None);
        }
    }

    [Fact]
    public async Task AddAsync_PersisteEAtribuiId()
    {
        await SeedUsersAsync(1);
        using var context = _factory.CreateContext();
        var repository = new TaskRepository(context);
        var task = TaskItem.Create(1, "Relatório", null, TaskPriority.Media, 4, BaseTime);

        await repository.AddAsync(task, CancellationToken.None);

        task.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdForUserAsync_ComTarefaDoDono_Retorna()
    {
        await SeedUsersAsync(1);
        int id;
        using (var seedContext = _factory.CreateContext())
        {
            var task = TaskItem.Create(1, "Relatório", null, TaskPriority.Media, 4, BaseTime);
            await new TaskRepository(seedContext).AddAsync(task, CancellationToken.None);
            id = task.Id;
        }

        using var context = _factory.CreateContext();
        var found = await new TaskRepository(context).GetByIdForUserAsync(id, 1, CancellationToken.None);

        found.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdForUserAsync_ComTarefaDeOutroUsuario_RetornaNull()
    {
        await SeedUsersAsync(2);
        int id;
        using (var seedContext = _factory.CreateContext())
        {
            var task = TaskItem.Create(1, "Relatório", null, TaskPriority.Media, 4, BaseTime);
            await new TaskRepository(seedContext).AddAsync(task, CancellationToken.None);
            id = task.Id;
        }

        using var context = _factory.CreateContext();
        var found = await new TaskRepository(context).GetByIdForUserAsync(id, 2, CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task ListForUserAsync_SemFiltro_RetornaTodasDoUsuarioOrdenadasPorMaisRecente()
    {
        await SeedUsersAsync(2);
        using (var seedContext = _factory.CreateContext())
        {
            var repo = new TaskRepository(seedContext);
            await repo.AddAsync(TaskItem.Create(1, "Primeira", null, TaskPriority.Baixa, 1, BaseTime), CancellationToken.None);
            await repo.AddAsync(TaskItem.Create(1, "Segunda", null, TaskPriority.Baixa, 1, BaseTime.AddMinutes(1)), CancellationToken.None);
            await repo.AddAsync(TaskItem.Create(2, "DeOutroUsuario", null, TaskPriority.Baixa, 1, BaseTime), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var items = await new TaskRepository(context).ListForUserAsync(1, status: null, CancellationToken.None);

        items.Should().HaveCount(2);
        items[0].Title.Should().Be("Segunda");
    }

    [Fact]
    public async Task ListForUserAsync_ComFiltroDeStatus_RetornaApenasOStatusPedido()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            var repo = new TaskRepository(seedContext);
            var done = TaskItem.Create(1, "Concluída", null, TaskPriority.Baixa, 1, BaseTime);
            done.MarkAsDone(BaseTime);
            await repo.AddAsync(done, CancellationToken.None);
            await repo.AddAsync(TaskItem.Create(1, "Pendente", null, TaskPriority.Baixa, 1, BaseTime), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var items = await new TaskRepository(context).ListForUserAsync(1, TaskItemStatus.Feito, CancellationToken.None);

        items.Should().ContainSingle().Which.Title.Should().Be("Concluída");
    }

    [Fact]
    public async Task FindInFocusAsync_ComUmaTarefaEmCurso_Retorna()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            var task = TaskItem.Create(1, "Em foco", null, TaskPriority.Baixa, 1, BaseTime);
            task.MarkAsFocused(BaseTime);
            await new TaskRepository(seedContext).AddAsync(task, CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var found = await new TaskRepository(context).FindInFocusAsync(1, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Title.Should().Be("Em foco");
    }

    [Fact]
    public async Task FindInFocusAsync_SemTarefaEmCurso_RetornaNull()
    {
        await SeedUsersAsync(1);
        using (var seedContext = _factory.CreateContext())
        {
            await new TaskRepository(seedContext).AddAsync(
                TaskItem.Create(1, "Pendente", null, TaskPriority.Baixa, 1, BaseTime), CancellationToken.None);
        }

        using var context = _factory.CreateContext();
        var found = await new TaskRepository(context).FindInFocusAsync(1, CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Remove()
    {
        await SeedUsersAsync(1);
        int id;
        using (var seedContext = _factory.CreateContext())
        {
            var task = TaskItem.Create(1, "Relatório", null, TaskPriority.Media, 4, BaseTime);
            await new TaskRepository(seedContext).AddAsync(task, CancellationToken.None);
            id = task.Id;
        }

        using (var context = _factory.CreateContext())
        {
            var repository = new TaskRepository(context);
            var task = await repository.GetByIdForUserAsync(id, 1, CancellationToken.None);
            await repository.DeleteAsync(task!, CancellationToken.None);
        }

        using var assertContext = _factory.CreateContext();
        var found = await new TaskRepository(assertContext).GetByIdForUserAsync(id, 1, CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_PersisteAlteracoesFeitasEmEntidadeRastreada()
    {
        await SeedUsersAsync(1);
        int id;
        using (var seedContext = _factory.CreateContext())
        {
            var task = TaskItem.Create(1, "Relatório", null, TaskPriority.Media, 4, BaseTime);
            await new TaskRepository(seedContext).AddAsync(task, CancellationToken.None);
            id = task.Id;
        }

        using (var context = _factory.CreateContext())
        {
            var repository = new TaskRepository(context);
            var task = await repository.GetByIdForUserAsync(id, 1, CancellationToken.None);
            task!.MarkAsDone(BaseTime.AddMinutes(5));
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        using var assertContext = _factory.CreateContext();
        var found = await new TaskRepository(assertContext).GetByIdForUserAsync(id, 1, CancellationToken.None);
        found!.Status.Should().Be(TaskItemStatus.Feito);
    }
}
