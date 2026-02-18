using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramBotFlow.Broadcasts.Domain;

namespace TelegramBotFlow.Broadcasts.Infrastructure.Configurations;

public sealed class UserSequenceProgressConfiguration : IEntityTypeConfiguration<UserSequenceProgress>
{
    public void Configure(EntityTypeBuilder<UserSequenceProgress> builder)
    {
        builder.ToTable("user_sequence_progress");

        builder.HasKey(x => new { x.UserId, x.SequenceId, x.StepId });

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.SequenceId)
            .HasColumnName("sequence_id")
            .IsRequired();

        builder.Property(x => x.StepId)
            .HasColumnName("step_id")
            .IsRequired();

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at")
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.SequenceId });
    }
}
