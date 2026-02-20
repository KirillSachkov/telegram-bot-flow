using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramBotFlow.Broadcasts.Domain;

namespace TelegramBotFlow.Broadcasts.Infrastructure.Configurations;

public sealed class BroadcastSequenceConfiguration : IEntityTypeConfiguration<BroadcastSequence>
{
    public void Configure(EntityTypeBuilder<BroadcastSequence> builder)
    {
        _ = builder.ToTable("broadcast_sequences");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("id");

        _ = builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        _ = builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        _ = builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        _ = builder.HasMany(x => x.Steps)
            .WithOne(x => x.Sequence)
            .HasForeignKey(x => x.SequenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
