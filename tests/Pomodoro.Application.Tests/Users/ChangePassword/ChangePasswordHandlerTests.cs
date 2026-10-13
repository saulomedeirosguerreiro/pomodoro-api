using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Users.ChangePassword;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Users.ChangePassword;

public class ChangePasswordHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ChangePasswordHandler _handler;
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public ChangePasswordHandlerTests()
    {
        _clock.UtcNow.Returns(BaseTime);
        _handler = new ChangePasswordHandler(_users, _hasher, _clock);
    }

    [Fact]
    public async Task HandleAsync_ComSenhaAtualCorreta_AtualizaSenhaESalva()
    {
        var user = User.Create("João", "joao@email.com", "hash-antigo", BaseTime.AddDays(-30));
        _users.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("SenhaAtual1", "hash-antigo").Returns(true);
        _hasher.Hash("SenhaNova123").Returns("hash-novo");

        await _handler.HandleAsync(1, new ChangePasswordRequest("SenhaAtual1", "SenhaNova123"), CancellationToken.None);

        user.PasswordHash.Should().Be("hash-novo");
        user.UpdatedAt.Should().Be(BaseTime);
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComSenhaAtualErrada_LancaInvalidCredentialsExceptionENaoAtualiza()
    {
        var user = User.Create("João", "joao@email.com", "hash-antigo", BaseTime.AddDays(-30));
        _users.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("errada", "hash-antigo").Returns(false);

        var act = () => _handler.HandleAsync(1, new ChangePasswordRequest("errada", "SenhaNova123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        user.PasswordHash.Should().Be("hash-antigo");
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComUsuarioInexistente_LancaNotFoundException()
    {
        _users.FindByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.HandleAsync(99, new ChangePasswordRequest("qualquer", "SenhaNova123"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
