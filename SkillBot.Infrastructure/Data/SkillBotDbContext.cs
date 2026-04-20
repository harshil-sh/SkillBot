using Microsoft.EntityFrameworkCore;
using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.Data;

public class SkillBotDbContext : DbContext
{
    public SkillBotDbContext(DbContextOptions<SkillBotDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChannelUser> ChannelUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Message)
                .IsRequired();

            entity.Property(c => c.Response)
                .IsRequired();

            entity.HasIndex(c => c.UserId);
            entity.HasIndex(c => c.ConversationId);
            entity.HasIndex(c => c.CreatedAt);
        });

        modelBuilder.Entity<ChannelUser>(entity =>
        {
            entity.HasKey(cu => cu.Id);

            entity.Property(cu => cu.ChannelName).IsRequired().HasMaxLength(50);
            entity.Property(cu => cu.ChannelUserId).IsRequired().HasMaxLength(256);
            entity.Property(cu => cu.SystemUserId).IsRequired();

            // Unique mapping per channel + channel-user pair
            entity.HasIndex(cu => new { cu.ChannelName, cu.ChannelUserId }).IsUnique();
            entity.HasIndex(cu => cu.SystemUserId);
        });
    }
}
