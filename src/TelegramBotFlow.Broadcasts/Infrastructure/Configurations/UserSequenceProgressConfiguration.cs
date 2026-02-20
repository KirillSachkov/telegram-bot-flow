using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramBotFlow.Broadcasts.Domain;

namespace TelegramBotFlow.Broadcasts.Infrastructure.Configurations;

public sealed class UserSequenceProgressConfiguration : IEntityTypeConfiguration<UserSequenceProgress>
{
    public void Configure(EntityTypeBuilder<UserSequenceProgress> builder)
    {
        _ = builder.ToTable("user_sequence_progress");

        _ = builder.HasKey(x => new { x.UserId, x.SequenceId, x.StepId });

        _ = builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        _ = builder.Property(x => x.SequenceId)
            .HasColumnName("sequence_id")
            .IsRequired();

        _ = builder.Property(x => x.StepId)
            .HasColumnName("step_id")
            .IsRequired();

        _ = builder.Property(x => x.SentAt)
            .HasColumnName("sent_at")
            .IsRequired();

        _ = builder.HasIndex(x => new { x.UserId, x.SequenceId });
    }
}
