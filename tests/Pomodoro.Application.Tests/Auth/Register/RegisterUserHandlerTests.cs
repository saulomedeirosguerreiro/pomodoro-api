using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Auth.Register;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Auth.Register;

public class RegisterUserHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new RegisterUserHandler(_users, _hasher, _clock);
    }

    [Fact]
    public async Task HandleAsync_ComEmailNovo_CriaUsuarioComSenhaHasheada()
    {
        _users.ExistsByEmailAsync("joao@email.com", Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash("Senha123").Returns("hash-fake");

        var response = await _handler.HandleAsync(
            new RegisterUserRequest("João", "joao@email.com", "Senha123"), CancellationToken.None);

        response.Should().BeEquivalentTo(new RegisterUserResponse(Id: 0, Name: "João", Email: "joao@email.com"));
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "joao@email.com" && u.PasswordHash == "hash-fake"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComEmailJaCadastrado_LancaConflictException()
    {
        _users.ExistsByEmailAsync("joao@email.com", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _handler.HandleAsync(
            new RegisterUserRequest("João", "joao@email.com", "Senha123"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
