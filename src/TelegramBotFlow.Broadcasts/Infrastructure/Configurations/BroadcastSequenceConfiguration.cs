using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramBotFlow.Broadcasts.Domain;

namespace TelegramBotFlow.Broadcasts.Infrastructure.Configurations;

public sealed class BroadcastSequenceConfiguration : IEntityTypeConfiguration<BroadcastSequence>
{
    public void Configure(EntityTypeBuilder<BroadcastSequence> builder)
    {
        builder.ToTable("broadcast_sequences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasMany(x => x.Steps)
            .WithOne(x => x.Sequence)
            .HasForeignKey(x => x.SequenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
