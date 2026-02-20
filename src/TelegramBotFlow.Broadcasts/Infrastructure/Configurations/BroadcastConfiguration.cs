using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramBotFlow.Broadcasts.Domain;

namespace TelegramBotFlow.Broadcasts.Infrastructure.Configurations;

public sealed class BroadcastConfiguration : IEntityTypeConfiguration<Broadcast>
{
    public void Configure(EntityTypeBuilder<Broadcast> builder)
    {
        _ = builder.ToTable("broadcasts");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("id");

        _ = builder.Property(x => x.FromChatId)
            .HasColumnName("from_chat_id")
            .IsRequired();

        _ = builder.Property(x => x.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        _ = builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        _ = builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        _ = builder.Property(x => x.SentAt)
            .HasColumnName("sent_at");

        _ = builder.Property(x => x.SuccessCount)
            .HasColumnName("success_count")
            .HasDefaultValue(0);

        _ = builder.Property(x => x.FailureCount)
            .HasColumnName("failure_count")
            .HasDefaultValue(0);
    }
}
