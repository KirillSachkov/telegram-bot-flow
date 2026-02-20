using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TelegramBotFlow.Core.Data.Configurations;

public sealed class BotSettingsConfiguration : IEntityTypeConfiguration<BotSettings>
{
    public void Configure(EntityTypeBuilder<BotSettings> builder)
    {
        _ = builder.ToTable("bot_settings");

        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        _ = builder.OwnsOne(x => x.Roadmap, nav =>
        {
            _ = nav.ToJson("roadmap");
            _ = nav.Property(r => r.SourceChatId).HasJsonPropertyName("source_chat_id");
            _ = nav.Property(r => r.SourceMessageId).HasJsonPropertyName("source_message_id");
        });
    }
}
