using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Infrastructure.Persistence.Configurations;

public sealed class PomodoroSessionConfiguration : IEntityTypeConfiguration<PomodoroSession>
{
    public void Configure(EntityTypeBuilder<PomodoroSession> builder)
    {
        builder.ToTable("pomodoros");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.DurationSeconds).IsRequired();
        builder.Property(s => s.StartedAt).IsRequired();
        builder.Property(s => s.CompletedAt).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasIndex(s => new { s.UserId, s.CreatedAt });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.TaskItemId).HasColumnName("task_id");

        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(s => s.TaskItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
