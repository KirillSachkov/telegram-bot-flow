using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramBotFlow.Broadcasts.Domain;

namespace TelegramBotFlow.Broadcasts.Infrastructure.Configurations;

public sealed class BroadcastSequenceStepConfiguration : IEntityTypeConfiguration<BroadcastSequenceStep>
{
    public void Configure(EntityTypeBuilder<BroadcastSequenceStep> builder)
    {
        _ = builder.ToTable("broadcast_sequence_steps");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("id");

        _ = builder.Property(x => x.SequenceId)
            .HasColumnName("sequence_id")
            .IsRequired();

        _ = builder.Property(x => x.Order)
            .HasColumnName("order")
            .IsRequired();

        _ = builder.Property(x => x.FromChatId)
            .HasColumnName("from_chat_id")
            .IsRequired();

        _ = builder.Property(x => x.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        _ = builder.Property(x => x.DelayAfterJoin)
            .HasColumnName("delay_after_join")
            .IsRequired();

        _ = builder.HasIndex(x => new { x.SequenceId, x.Order })
            .IsUnique();
    }
}
