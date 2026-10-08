using Microsoft.EntityFrameworkCore;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Infrastructure.Persistence;

public sealed class PomodoroDbContext : DbContext
{
    public PomodoroDbContext(DbContextOptions<PomodoroDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<PomodoroSession> PomodoroSessions => Set<PomodoroSession>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

    public DbSet<GuestImport> GuestImports => Set<GuestImport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PomodoroDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
