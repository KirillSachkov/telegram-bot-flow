using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TelegramBotFlow.Core.Data.Configurations;

public sealed class BotUserConfiguration : IEntityTypeConfiguration<BotUser>
{
    public void Configure(EntityTypeBuilder<BotUser> builder)
    {
        _ = builder.ToTable("users");

        _ = builder.HasKey(x => x.TelegramId);

        _ = builder.Property(x => x.TelegramId)
            .HasColumnName("telegram_id")
            .ValueGeneratedNever();

        _ = builder.Property(x => x.JoinedAt)
            .HasColumnName("joined_at")
            .IsRequired();

        _ = builder.Property(x => x.IsBlocked)
            .HasColumnName("is_blocked")
            .HasDefaultValue(false);
    }
}
