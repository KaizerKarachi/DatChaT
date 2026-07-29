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
}
