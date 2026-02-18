using Microsoft.EntityFrameworkCore;
using TelegramBotFlow.Broadcasts.Domain;

namespace TelegramBotFlow.Broadcasts.Infrastructure;

public sealed class BroadcastsDbContext(DbContextOptions<BroadcastsDbContext> options)
    : DbContext(options)
{
    public DbSet<Broadcast> Broadcasts => Set<Broadcast>();

    public DbSet<BroadcastSequence> Sequences => Set<BroadcastSequence>();

    public DbSet<BroadcastSequenceStep> SequenceSteps => Set<BroadcastSequenceStep>();

    public DbSet<UserSequenceProgress> UserSequenceProgress => Set<UserSequenceProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BroadcastsDbContext).Assembly);
    }
}
