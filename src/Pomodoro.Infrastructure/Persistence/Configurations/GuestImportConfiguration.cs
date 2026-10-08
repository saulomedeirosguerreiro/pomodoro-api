using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Infrastructure.Persistence.Configurations;

public sealed class GuestImportConfiguration : IEntityTypeConfiguration<GuestImport>
{
    public void Configure(EntityTypeBuilder<GuestImport> builder)
    {
        builder.ToTable("guest_imports");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.GuestId).IsRequired().HasMaxLength(100);
        builder.Property(g => g.ImportedAt).IsRequired();
        builder.Property(g => g.AchievementsUnlockedJson).IsRequired();
        builder.Property(g => g.SkippedItemsJson).IsRequired();

        // Idempotência (D2): o mesmo guestId nunca é processado duas vezes para o mesmo usuário.
        builder.HasIndex(g => new { g.UserId, g.GuestId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
