using FamilyChat.Data;
using FamilyChat.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Services;

public class ChatService
{
    private readonly ChatDbContext _db;
    private readonly ILogger<ChatService> _logger;

    // Кеш онлайн-пользователей (чтобы не дёргать БД каждый раз)
    private static readonly HashSet<string> _onlineCache = new();
    private static readonly object _cacheLock = new();

    public ChatService(ChatDbContext db, ILogger<ChatService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // === ПОЛЬЗОВАТЕЛИ ===

    public async Task<User?> FindByNickname(string nickname)
        => await _db.Users.FirstOrDefaultAsync(u => u.Nickname == nickname);

    public async Task<User?> FindByConnectionId(string connectionId)
        => await _db.Users.FirstOrDefaultAsync(u => u.ConnectionId == connectionId);

    public async Task<User> CreateUser(string nickname, string passwordHash)
    {
        var user = new User
        {
            Nickname = nickname,
            PasswordHash = passwordHash,
            IsApproved = true,
            IsAdmin = false,
            SessionToken = Guid.NewGuid().ToString()
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _logger.LogInformation("✅ Создан пользователь: {Nick}", nickname);
        return user;
    }

    public async Task SetUserOnline(User user, string connectionId)
    {
        user.ConnectionId = connectionId;
        user.IsOnline = true;
        user.LastSeen = DateTime.UtcNow;
        user.SessionToken = Guid.NewGuid().ToString();
        await _db.SaveChangesAsync();

        lock (_cacheLock)
            _onlineCache.Add(user.Nickname);
    }

    public async Task SetUserOffline(string connectionId)
    {
        var user = await FindByConnectionId(connectionId);
        if (user != null)
        {
            user.ConnectionId = null;
            user.IsOnline = false;
            user.LastSeen = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            lock (_cacheLock)
                _onlineCache.Remove(user.Nickname);
        }
    }

    public List<string> GetOnlineUsers()
    {
        lock (_cacheLock)
            return _onlineCache.ToList();
    }

    public void RebuildOnlineCache()
    {
        var online = _db.Users.Where(u => u.IsOnline).Select(u => u.Nickname).ToList();
        lock (_cacheLock)
        {
            _onlineCache.Clear();
            foreach (var u in online) _onlineCache.Add(u);
        }
        _logger.LogInformation("🔄 Кеш онлайн восстановлен: {Count} пользователей", online.Count);
    }

    // === СООБЩЕНИЯ ===

    public async Task<ChatMessage> SaveMessage(string user, string text, string? fileUrl = null, string? fileType = null)
    {
        var msg = new ChatMessage
        {
            User = user,
            Text = text,
            FileUrl = fileUrl,
            FileType = fileType,
            Timestamp = DateTime.UtcNow
        };
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();
        return msg;
    }

    public async Task MarkDeleted(int messageId)
    {
        var msg = await _db.Messages.FindAsync(messageId);
        if (msg != null)
        {
            msg.IsDeleted = true;
            msg.Text = "";
            msg.FileUrl = null;
            msg.FileType = null;
            await _db.SaveChangesAsync();
        }
    }

    // Пагинация: берём последние N сообщений в правильном порядке
    public async Task<List<ChatMessage>> GetRecentMessages(int count = 100)
    {
        return await _db.Messages
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ChatMessage>> SearchMessages(string searchText, int limit = 20)
    {
        return await _db.Messages
            .Where(m => m.Text.Contains(searchText) && !m.IsDeleted)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    // === ЗАКРЕПЛЁННЫЕ ===

    public async Task<PinnedMessage?> GetLastPinned()
        => await _db.PinnedMessages.OrderByDescending(p => p.PinnedAt).FirstOrDefaultAsync();

    public async Task<PinnedMessage> PinMessage(int messageId, string pinnedBy)
    {
        // Удаляем старое
        var old = await GetLastPinned();
        if (old != null)
        {
            _db.PinnedMessages.Remove(old);
            var oldMsg = await _db.Messages.FindAsync(old.MessageId);
            if (oldMsg != null) oldMsg.IsPinned = false;
        }

        var pinned = new PinnedMessage
        {
            MessageId = messageId,
            PinnedBy = pinnedBy,
            PinnedAt = DateTime.UtcNow
        };
        _db.PinnedMessages.Add(pinned);

        var msg = await _db.Messages.FindAsync(messageId);
        if (msg != null) msg.IsPinned = true;

        await _db.SaveChangesAsync();
        return pinned;
    }

    public async Task UnpinLast()
    {
        var pinned = await GetLastPinned();
        if (pinned != null)
        {
            _db.PinnedMessages.Remove(pinned);
            var msg = await _db.Messages.FindAsync(pinned.MessageId);
            if (msg != null) msg.IsPinned = false;
            await _db.SaveChangesAsync();
        }
    }

    // === ЛИЧНЫЕ СООБЩЕНИЯ ===

    public async Task<PrivateMessage> SavePrivateMessage(string sender, string receiver, string text)
    {
        var pm = new PrivateMessage
        {
            SenderId = sender,
            ReceiverId = receiver,
            Text = text,
            Timestamp = DateTime.UtcNow,
            IsRead = false
        };
        _db.PrivateMessages.Add(pm);
        await _db.SaveChangesAsync();
        return pm;
    }
}
