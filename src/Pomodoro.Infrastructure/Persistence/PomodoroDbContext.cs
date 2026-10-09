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
        modelBuilder.HasPostgresExtension("citext"); // necessário p/ UserConfiguration.HasColumnType("citext")
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PomodoroDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Sem o HaveColumnType explícito, o provider Npgsql mapeia DateTime para "timestamp with
        // time zone" por padrão (desde a revisão de tipos de data/hora do Npgsql 6+) — tipo que EXIGE
        // Kind=Utc (ou Local) na escrita e rejeitaria o Kind=Unspecified das datas de sessões
        // importadas do guest (ImportGuestDataHandler). "timestamp without time zone" é o tipo que o
        // achado A do plano de migração pressupõe: aceita qualquer Kind na escrita, e o
        // UtcDateTimeConverter garante Kind=Utc na leitura.
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>()
            .HaveColumnType("timestamp without time zone");
    }
}
