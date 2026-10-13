using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Users.GetProfile;
using Pomodoro.Application.Users.UpdateName;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Users.UpdateName;

public class UpdateNameHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly UpdateNameHandler _handler;
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public UpdateNameHandlerTests()
    {
        _clock.UtcNow.Returns(BaseTime);
        _handler = new UpdateNameHandler(_users, _sessions, _clock);
    }

    [Fact]
    public async Task HandleAsync_ComNomeValido_AtualizaNomeESalvaERetornaPerfil()
    {
        var user = User.Create("João", "joao@email.com", "hash", BaseTime.AddDays(-30));
        _users.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _sessions.CountCompletedFocusAsync(1, Arg.Any<CancellationToken>()).Returns(5);

        var response = await _handler.HandleAsync(1, new UpdateNameRequest("João Silva"), CancellationToken.None);

        user.Name.Should().Be("João Silva");
        user.UpdatedAt.Should().Be(BaseTime);
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        response.Should().BeEquivalentTo(new UserProfileResponse(0, "João Silva", "joao@email.com", 5));
    }

    [Fact]
    public async Task HandleAsync_ComUsuarioInexistente_LancaNotFoundException()
    {
        _users.FindByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.HandleAsync(99, new UpdateNameRequest("João"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
