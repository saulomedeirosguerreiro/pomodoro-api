using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Users.GetProfile;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Users.GetProfile;

public class GetUserProfileHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly GetUserProfileHandler _handler;

    public GetUserProfileHandlerTests()
    {
        _handler = new GetUserProfileHandler(_users, _sessions);
    }

    [Fact]
    public async Task HandleAsync_ComUsuarioExistente_RetornaPerfilComContador()
    {
        var user = User.Create("João", "joao@email.com", "hash", DateTime.UtcNow);
        _users.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _sessions.CountCompletedFocusAsync(1, Arg.Any<CancellationToken>()).Returns(3);

        var response = await _handler.HandleAsync(1, CancellationToken.None);

        response.Should().BeEquivalentTo(new UserProfileResponse(0, "João", "joao@email.com", 3));
    }

    [Fact]
    public async Task HandleAsync_ComUsuarioInexistente_LancaNotFoundException()
    {
        _users.FindByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.HandleAsync(99, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
