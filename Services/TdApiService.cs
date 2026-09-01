using FamilyChat.Constants;
using FamilyChat.Data;
using FamilyChat.DTO;
using FamilyChat.Interfaces;
using FamilyChat.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Services;

public class TdApiService : ITdApiService
{
    private readonly ChatDbContext _db;
    private readonly IChatService _chats;
    private readonly IMessageService _pms;
    private readonly IUserService _users;

    public TdApiService(ChatDbContext db, IChatService chats, IMessageService pms, IUserService users)
    {
        _db = db;
        _chats = chats;
        _pms = pms;
        _users = users;
    }

    public async Task<List<TdChatDto>> GetChatsAsync(User me)
    {
        var people = await _users.GetUsersAsync();
        var familyLast = (await _chats.GetRecentMessagesAsync(1)).LastOrDefault();
        var inbox = await InboxForUserAsync(me.Nickname);
        var lastByPeer = await LastPrivateByPeerAsync(me.Nickname);
        var unreadByPeer = await UnreadPrivateByPeerAsync(me.Nickname);

        var list = new List<TdChatDto>
        {
            new()
            {
                Id = ChatIds.Family,
                Type = "group",
                Title = "Family",
                Nickname = "family",
                DisplayName = "Family",
                IsOnline = true,
                Status = "общий чат",
                UnreadCount = inbox.GetValueOrDefault(ChatIds.Family),
                LastMessage = familyLast == null ? null : MapFamily(familyLast),
                Order = familyLast?.Timestamp.Ticks ?? 0
            }
        };

        foreach (var person in people)
        {
            if (string.Equals(person.Nickname, me.Nickname, StringComparison.OrdinalIgnoreCase))
                continue;

            lastByPeer.TryGetValue(person.Nickname, out var last);
            unreadByPeer.TryGetValue(person.Nickname, out var unread);
            var chatId = ChatIds.Private(person.Nickname);
            if (inbox.TryGetValue(chatId, out var extra) && extra > unread)
                unread = extra;

            list.Add(new TdChatDto
            {
                Id = chatId,
                Type = "private",
                Title = person.DisplayName,
                Nickname = person.Nickname,
                DisplayName = person.DisplayName,
                IsOnline = person.IsOnline,
                Status = person.Status,
                UnreadCount = unread,
                LastMessage = last == null ? null : MapPrivate(last, person.Nickname),
                Order = last?.Timestamp.Ticks ?? person.LastSeen.Ticks
            });
        }

        return list.OrderByDescending(c => c.Id == ChatIds.Family)
            .ThenByDescending(c => c.Order)
            .ToList();
    }

    public async Task<TdChatHistoryDto> GetChatHistoryAsync(User me, string chatId, int limit = AppConstants.HistoryLimit)
    {
        if (ChatIds.IsFamily(chatId))
        {
            var history = await _chats.GetRecentMessagesAsync(limit);
            return new TdChatHistoryDto
            {
                ChatId = ChatIds.Family,
                Messages = history.Select(MapFamily).ToList()
            };
        }

        var peer = ChatIds.PeerNickname(chatId) ?? throw new ArgumentException("Неизвестный чат");
        var other = await _users.FindByNicknameAsync(peer) ?? throw new ArgumentException("Пользователь не найден");
        var pms = await _pms.GetPrivateMessagesAsync(me.Nickname, other.Nickname);
        if (pms.Count > limit)
            pms = pms.Skip(pms.Count - limit).ToList();
        return new TdChatHistoryDto
        {
            ChatId = ChatIds.Private(other.Nickname),
            Messages = pms.Select(m => MapPrivate(m, other.Nickname)).ToList()
        };
    }

    public async Task<TdMessageDto> SendTextAsync(User me, string chatId, string text)
    {
        var check = InputValidator.ValidateMessage(text);
        if (!check.ok) throw new InvalidOperationException(check.error);

        if (ChatIds.IsFamily(chatId))
        {
            var saved = await _chats.SaveMessageAsync(me.Nickname, text);
            return MapFamily(saved);
        }

        var peer = await RequirePeerAsync(chatId);
        var pm = await _pms.SavePrivateMessageAsync(me.Nickname, peer.Nickname, text);
        return MapPrivate(pm, peer.Nickname);
    }

    public async Task<TdMessageDto> SendFileAsync(User me, string chatId, string text, string fileUrl, string fileType)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new InvalidOperationException("Нет файла");

        if (ChatIds.IsFamily(chatId))
        {
            var saved = await _chats.SaveMessageAsync(me.Nickname, text ?? "", fileUrl, fileType);
            return MapFamily(saved);
        }

        var peer = await RequirePeerAsync(chatId);
        var pm = await _pms.SavePrivateMessageAsync(me.Nickname, peer.Nickname, text ?? "", fileUrl, fileType);
        return MapPrivate(pm, peer.Nickname);
    }

    public async Task ViewMessagesAsync(User me, string chatId)
    {
        await SetUnreadAsync(me.Nickname, chatId, 0);
        if (ChatIds.IsFamily(chatId)) return;

        var peer = ChatIds.PeerNickname(chatId);
        if (peer == null) return;
        await _db.PrivateMessages
            .Where(m => m.ReceiverId == me.Nickname && m.SenderId == peer && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
    }

    public async Task IncrementUnreadAsync(string chatId, string exceptNickname)
    {
        if (ChatIds.IsFamily(chatId))
        {
            var nicks = await _db.Users.AsNoTracking()
                .Where(u => u.Nickname != exceptNickname)
                .Select(u => u.Nickname)
                .ToListAsync();
            foreach (var nick in nicks)
                await AddUnreadAsync(nick, ChatIds.Family, 1);
            return;
        }

        var peer = ChatIds.PeerNickname(chatId);
        if (!string.IsNullOrEmpty(peer))
            await AddUnreadAsync(peer, ChatIds.Private(exceptNickname), 1);
    }

    private async Task<User> RequirePeerAsync(string chatId)
    {
        var nick = ChatIds.PeerNickname(chatId) ?? throw new InvalidOperationException("Неизвестный чат");
        return await _users.FindByNicknameAsync(nick) ?? throw new InvalidOperationException("Пользователь не найден");
    }

    private Task<Dictionary<string, int>> InboxForUserAsync(string nickname) =>
        _db.ChatInboxes.AsNoTracking()
            .Where(x => x.UserNickname == nickname)
            .ToDictionaryAsync(x => x.ChatId, x => x.UnreadCount, StringComparer.OrdinalIgnoreCase);

    private async Task SetUnreadAsync(string nickname, string chatId, int count)
    {
        var row = await _db.ChatInboxes
            .FirstOrDefaultAsync(x => x.UserNickname == nickname && x.ChatId == chatId);
        if (row == null)
        {
            _db.ChatInboxes.Add(new ChatInbox { UserNickname = nickname, ChatId = chatId, UnreadCount = count });
        }
        else
        {
            row.UnreadCount = count;
        }
        await _db.SaveChangesAsync();
    }

    private async Task AddUnreadAsync(string nickname, string chatId, int delta)
    {
        var row = await _db.ChatInboxes
            .FirstOrDefaultAsync(x => x.UserNickname == nickname && x.ChatId == chatId);
        if (row == null)
            _db.ChatInboxes.Add(new ChatInbox { UserNickname = nickname, ChatId = chatId, UnreadCount = delta });
        else
            row.UnreadCount += delta;
        await _db.SaveChangesAsync();
    }

    private async Task<Dictionary<string, PrivateMessage>> LastPrivateByPeerAsync(string me)
    {
        var rows = await _db.PrivateMessages.AsNoTracking()
            .Where(m => m.SenderId == me || m.ReceiverId == me)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();

        var map = new Dictionary<string, PrivateMessage>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var peer = row.SenderId == me ? row.ReceiverId : row.SenderId;
            if (!map.ContainsKey(peer))
                map[peer] = row;
        }
        return map;
    }

    private async Task<Dictionary<string, int>> UnreadPrivateByPeerAsync(string me)
    {
        return await _db.PrivateMessages.AsNoTracking()
            .Where(m => m.ReceiverId == me && !m.IsRead)
            .GroupBy(m => m.SenderId)
            .Select(g => new { Peer = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Peer, x => x.Count, StringComparer.OrdinalIgnoreCase);
    }

    private static TdMessageDto MapFamily(ChatMessage message) => new()
    {
        Id = "f-" + message.Id,
        ChatId = ChatIds.Family,
        SenderId = message.User,
        Nickname = message.User,
        Text = message.Text,
        FileUrl = message.FileUrl,
        FileType = message.FileType,
        Time = message.Timestamp.ToString("HH:mm"),
        Timestamp = message.Timestamp,
        IsDeleted = message.IsDeleted,
        IsPinned = message.IsPinned
    };

    private static TdMessageDto MapPrivate(PrivateMessage message, string peerNickname) => new()
    {
        Id = "p-" + message.Id,
        ChatId = ChatIds.Private(peerNickname),
        SenderId = message.SenderId,
        Nickname = message.SenderId,
        Text = message.Text,
        FileUrl = message.FileUrl,
        FileType = message.FileType,
        Time = message.Timestamp.ToString("HH:mm"),
        Timestamp = message.Timestamp
    };
}