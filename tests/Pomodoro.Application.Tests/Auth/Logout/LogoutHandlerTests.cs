using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Auth.Logout;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Auth.Logout;

public class LogoutHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly LogoutHandler _handler;
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public LogoutHandlerTests()
    {
        _clock.UtcNow.Returns(BaseTime);
        _handler = new LogoutHandler(_refreshTokens, _tokenService, _clock);
    }

    [Fact]
    public async Task HandleAsync_ComTokenValido_Revoga()
    {
        var token = RefreshToken.Create(1, "hash", BaseTime.AddDays(-1), BaseTime.AddDays(29));
        _tokenService.HashRefreshToken("raw").Returns("hash");
        _refreshTokens.FindByHashAsync("hash", Arg.Any<CancellationToken>()).Returns(token);

        await _handler.HandleAsync("raw", CancellationToken.None);

        token.RevokedAt.Should().Be(BaseTime);
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SemCookie_NaoFazNada()
    {
        await _handler.HandleAsync(null, CancellationToken.None);

        await _refreshTokens.DidNotReceive().FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComTokenInexistente_NaoLancaEhIdempotente()
    {
        _tokenService.HashRefreshToken("raw").Returns("hash");
        _refreshTokens.FindByHashAsync("hash", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var act = () => _handler.HandleAsync("raw", CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _refreshTokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
