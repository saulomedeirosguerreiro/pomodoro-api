using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pomodoro.Infrastructure.Persistence;

/// <summary>
/// Usada apenas em design-time por `dotnet ef migrations add` fora do host da Api.
/// A string de conexão real em runtime vem de `ConnectionStrings:Default` (US-21).
/// </summary>
public sealed class PomodoroDbContextFactory : IDesignTimeDbContextFactory<PomodoroDbContext>
{
    public PomodoroDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Data Source=pomodoro.design.db";

        var optionsBuilder = new DbContextOptionsBuilder<PomodoroDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new PomodoroDbContext(optionsBuilder.Options);
    }
}
