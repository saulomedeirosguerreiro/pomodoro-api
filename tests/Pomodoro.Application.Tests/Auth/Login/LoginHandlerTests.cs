using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Auth.Login;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Auth.Login;

public class LoginHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly LoginHandler _handler;
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public LoginHandlerTests()
    {
        _clock.UtcNow.Returns(BaseTime);
        _handler = new LoginHandler(_users, _hasher, _tokenService, _refreshTokens, _clock);
    }

    private static User BuildUser() =>
        User.Create("João", "joao@email.com", "hash-real", DateTime.UtcNow);

    [Fact]
    public async Task HandleAsync_ComCredenciaisValidas_RetornaTokensEUsuario()
    {
        var user = BuildUser();
        var refreshExpiresAt = BaseTime.AddDays(30);
        _users.FindByEmailAsync("joao@email.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("Senha123", "hash-real").Returns(true);
        _tokenService.GenerateToken(user).Returns("jwt-fake");
        _tokenService.GenerateRefreshToken(BaseTime).Returns(new RefreshTokenData("refresh-raw", "refresh-hash", refreshExpiresAt));

        var result = await _handler.HandleAsync(
            new LoginRequest("joao@email.com", "Senha123"), CancellationToken.None);

        result.Should().BeEquivalentTo(
            new LoginResult("jwt-fake", "refresh-raw", refreshExpiresAt, new LoginUserSummary(0, "João", "joao@email.com")));
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.TokenHash == "refresh-hash" && t.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComEmailInexistente_LancaInvalidCredentialsException()
    {
        _users.FindByEmailAsync("naoexiste@email.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.HandleAsync(
            new LoginRequest("naoexiste@email.com", "Senha123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task HandleAsync_ComSenhaErrada_LancaInvalidCredentialsException()
    {
        var user = BuildUser();
        _users.FindByEmailAsync("joao@email.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("errada", "hash-real").Returns(false);

        var act = () => _handler.HandleAsync(
            new LoginRequest("joao@email.com", "errada"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }
}
