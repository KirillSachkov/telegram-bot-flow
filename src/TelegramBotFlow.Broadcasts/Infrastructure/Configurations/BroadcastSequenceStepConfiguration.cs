using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramBotFlow.Broadcasts.Domain;

namespace TelegramBotFlow.Broadcasts.Infrastructure.Configurations;

public sealed class BroadcastSequenceStepConfiguration : IEntityTypeConfiguration<BroadcastSequenceStep>
{
    public void Configure(EntityTypeBuilder<BroadcastSequenceStep> builder)
    {
        builder.ToTable("broadcast_sequence_steps");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.SequenceId)
            .HasColumnName("sequence_id")
            .IsRequired();

        builder.Property(x => x.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(x => x.FromChatId)
            .HasColumnName("from_chat_id")
            .IsRequired();

        builder.Property(x => x.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        builder.Property(x => x.DelayAfterJoin)
            .HasColumnName("delay_after_join")
            .IsRequired();

        builder.HasIndex(x => new { x.SequenceId, x.Order })
            .IsUnique();
    }
}
