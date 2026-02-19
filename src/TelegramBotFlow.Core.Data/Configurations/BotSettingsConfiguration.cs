using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TelegramBotFlow.Core.Data.Configurations;

public sealed class BotSettingsConfiguration : IEntityTypeConfiguration<BotSettings>
{
    public void Configure(EntityTypeBuilder<BotSettings> builder)
    {
        builder.ToTable("bot_settings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.OwnsOne(x => x.Roadmap, nav =>
        {
            nav.ToJson("roadmap");
            nav.Property(r => r.SourceChatId).HasJsonPropertyName("source_chat_id");
            nav.Property(r => r.SourceMessageId).HasJsonPropertyName("source_message_id");
        });
    }
}
