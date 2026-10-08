using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Users.Delete;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Users.Delete;

public class DeleteAccountHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly DeleteAccountHandler _handler;
    private static readonly DateTime UtcNow = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public DeleteAccountHandlerTests()
    {
        _handler = new DeleteAccountHandler(_users, _hasher);
    }

    [Fact]
    public async Task HandleAsync_ComSenhaCorreta_ExcluiOUsuario()
    {
        var user = User.Create("João", "joao@email.com", "hash-real", UtcNow);
        _users.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("Senha123", "hash-real").Returns(true);

        await _handler.HandleAsync(1, new DeleteAccountRequest("Senha123"), CancellationToken.None);

        await _users.Received(1).DeleteAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComSenhaErrada_LancaInvalidCredentialsExceptionENaoExclui()
    {
        var user = User.Create("João", "joao@email.com", "hash-real", UtcNow);
        _users.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("errada", "hash-real").Returns(false);

        var act = () => _handler.HandleAsync(1, new DeleteAccountRequest("errada"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        await _users.DidNotReceive().DeleteAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComUsuarioInexistente_LancaNotFoundException()
    {
        _users.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.HandleAsync(1, new DeleteAccountRequest("qualquer"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
