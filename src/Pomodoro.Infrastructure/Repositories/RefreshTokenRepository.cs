using Microsoft.EntityFrameworkCore;
using Pomodoro.Application.Abstractions;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Persistence;

namespace Pomodoro.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly PomodoroDbContext _db;

    public RefreshTokenRepository(PomodoroDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> FindByHashAsync(string hash, CancellationToken cancellationToken) =>
        _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

    public async Task RevokeAllForUserAsync(int userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.Revoke(utcNow);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
