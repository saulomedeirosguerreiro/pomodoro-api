using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Auth.Refresh;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Auth.Refresh;

public class RefreshTokenHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly RefreshTokenHandler _handler;
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public RefreshTokenHandlerTests()
    {
        _clock.UtcNow.Returns(BaseTime);
        _handler = new RefreshTokenHandler(_refreshTokens, _users, _tokenService, _clock);
    }

    private static User BuildUser() => User.Create("João", "joao@email.com", "hash", BaseTime.AddDays(-30));

    [Fact]
    public async Task HandleAsync_ComTokenValido_RevogaOAntigoEEmiteNovoPar()
    {
        var user = BuildUser();
        var existing = RefreshToken.Create(user.Id, "hash-antigo", BaseTime.AddDays(-1), BaseTime.AddDays(29));
        var newData = new RefreshTokenData("raw-novo", "hash-novo", BaseTime.AddDays(30));

        _tokenService.HashRefreshToken("raw-antigo").Returns("hash-antigo");
        _refreshTokens.FindByHashAsync("hash-antigo", Arg.Any<CancellationToken>()).Returns(existing);
        _users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tokenService.GenerateRefreshToken(BaseTime).Returns(newData);
        _tokenService.GenerateToken(user).Returns("access-novo");

        var result = await _handler.HandleAsync("raw-antigo", CancellationToken.None);

        result.Should().Be(new RefreshResult("access-novo", "raw-novo", BaseTime.AddDays(30)));
        existing.RevokedAt.Should().Be(BaseTime);
        existing.ReplacedByTokenHash.Should().Be("hash-novo");
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.TokenHash == "hash-novo"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComTokenInexistente_LancaInvalidCredentialsException()
    {
        _tokenService.HashRefreshToken("desconhecido").Returns("hash-desconhecido");
        _refreshTokens.FindByHashAsync("hash-desconhecido", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var act = () => _handler.HandleAsync("desconhecido", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task HandleAsync_ComTokenExpirado_LancaInvalidCredentialsException()
    {
        var user = BuildUser();
        var expired = RefreshToken.Create(user.Id, "hash-expirado", BaseTime.AddDays(-31), BaseTime.AddDays(-1));
        _tokenService.HashRefreshToken("raw-expirado").Returns("hash-expirado");
        _refreshTokens.FindByHashAsync("hash-expirado", Arg.Any<CancellationToken>()).Returns(expired);

        var act = () => _handler.HandleAsync("raw-expirado", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task HandleAsync_ComTokenJaRevogado_DetectaReusoERevogaTodasAsSessoes()
    {
        var user = BuildUser();
        var reused = RefreshToken.Create(user.Id, "hash-reusado", BaseTime.AddDays(-2), BaseTime.AddDays(28));
        reused.Revoke(BaseTime.AddDays(-1), "hash-ja-rotacionado");
        _tokenService.HashRefreshToken("raw-reusado").Returns("hash-reusado");
        _refreshTokens.FindByHashAsync("hash-reusado", Arg.Any<CancellationToken>()).Returns(reused);

        var act = () => _handler.HandleAsync("raw-reusado", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        await _refreshTokens.Received(1).RevokeAllForUserAsync(user.Id, BaseTime, Arg.Any<CancellationToken>());
    }
}
