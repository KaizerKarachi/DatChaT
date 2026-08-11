using Microsoft.EntityFrameworkCore;
using FamilyChat.Models;

namespace FamilyChat.Data;

public class ChatDbContext : DbContext
{
    public DbSet<ChatMessage> Messages { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<PinnedMessage> PinnedMessages { get; set; }
    public DbSet<PrivateMessage> PrivateMessages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql("Host=localhost;Database=familychat;Username=postgres;Password=postgres");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Индексы для таблицы Users
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Nickname)
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.SessionToken);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.IsApproved);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.LastSeen);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.IsOnline);

        // Индексы для таблицы ChatMessages
        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => m.Timestamp);
        
        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => m.IsDeleted);
        
        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => m.User);
        
        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => m.IsPinned);

        // Индексы для таблицы PrivateMessages
        modelBuilder.Entity<PrivateMessage>()
            .HasIndex(pm => pm.SenderId);
        
        modelBuilder.Entity<PrivateMessage>()
            .HasIndex(pm => pm.ReceiverId);
        
        modelBuilder.Entity<PrivateMessage>()
            .HasIndex(pm => pm.Timestamp);
        
        modelBuilder.Entity<PrivateMessage>()
            .HasIndex(pm => pm.IsRead);
        
        modelBuilder.Entity<PrivateMessage>()
            .HasIndex(pm => new { pm.SenderId, pm.ReceiverId, pm.Timestamp });

        // Индексы для таблицы PinnedMessages
        modelBuilder.Entity<PinnedMessage>()
            .HasIndex(p => p.MessageId);
        
        modelBuilder.Entity<PinnedMessage>()
            .HasIndex(p => p.PinnedBy);
        
        modelBuilder.Entity<PinnedMessage>()
            .HasIndex(p => p.PinnedAt);
    }
}
