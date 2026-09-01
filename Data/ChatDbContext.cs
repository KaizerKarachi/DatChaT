using Microsoft.EntityFrameworkCore;
using FamilyChat.Models;

namespace FamilyChat.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<ChatMessage> Messages { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<PinnedMessage> PinnedMessages { get; set; } = null!;
    public DbSet<PrivateMessage> PrivateMessages { get; set; } = null!;
    public DbSet<ChatInbox> ChatInboxes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Nickname)
            .IsUnique();

        modelBuilder.Entity<ChatInbox>()
            .HasIndex(x => new { x.UserNickname, x.ChatId })
            .IsUnique();

        modelBuilder.Entity<PinnedMessage>()
            .HasOne(p => p.Message)
            .WithMany()
            .HasForeignKey(p => p.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
