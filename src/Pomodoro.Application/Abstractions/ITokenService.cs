using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Abstractions;

public interface ITokenService
{
    string GenerateToken(User user);
}
