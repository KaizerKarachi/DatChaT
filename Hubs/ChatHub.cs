using Microsoft.AspNetCore.SignalR;
using FamilyChat.Data;
using FamilyChat.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Hubs;

public class ChatHub : Hub
{
    private readonly ChatDbContext _db;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ChatDbContext db, ILogger<ChatHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RegisterOrLogin(string nickname, string password)
    {
        if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(password))
            throw new HubException("Введите ник и пароль");

        if (!nickname.StartsWith('#')) nickname = "#" + nickname;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Nickname == nickname);

        if (user == null)
        {
            _logger.LogInformation($"[НОВЫЙ] Регистрация: {nickname}");
            user = new User
            {
                Nickname = nickname,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsApproved = true,
                IsAdmin = false,
                SessionToken = Guid.NewGuid().ToString()
            };
            _db.Users.Add(user);
        }
        else
        {
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new HubException("Неверный пароль!");
            
            user.SessionToken = Guid.NewGuid().ToString();
        }

        await FinalizeLogin(user, true);
    }

    public async Task JoinByToken(string nickname, string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(sessionToken))
            throw new HubException("Неверные данные");

        if (!nickname.StartsWith('#')) nickname = "#" + nickname;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Nickname == nickname);
        
        if (user == null || user.SessionToken != sessionToken)
            throw new HubException("Сессия истекла");

        await FinalizeLogin(user, false);
    }

    private async Task FinalizeLogin(User user, bool generateNewToken)
    {
        if (!string.IsNullOrEmpty(user.ConnectionId) && user.ConnectionId != Context.ConnectionId)
            await Clients.Client(user.ConnectionId).SendAsync("ForceLogout", "Вы вошли с другого устройства");

        if (generateNewToken)
            user.SessionToken = Guid.NewGuid().ToString();

        user.ConnectionId = Context.ConnectionId;
        user.IsOnline = true;
        user.LastSeen = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        string displayNick = user.Nickname.StartsWith('#') ? user.Nickname.Substring(1) : user.Nickname;
        
        await Clients.All.SendAsync("UserJoined", displayNick);
        
        // Отправляем закреплённое сообщение
        var pinnedMessage = await _db.PinnedMessages.OrderByDescending(p => p.PinnedAt).FirstOrDefaultAsync();
        if (pinnedMessage != null)
        {
            var msg = await _db.Messages.FirstOrDefaultAsync(m => m.Id == pinnedMessage.MessageId);
            if (msg != null)
            {
                string userNick = msg.User.StartsWith('#') ? msg.User.Substring(1) : msg.User;
                var pinnedData = new Dictionary<string, object> {
                    ["Id"] = msg.Id,
                    ["Nickname"] = userNick,
                    ["Text"] = msg.IsDeleted ? "Сообщение удалено" : msg.Text,
                    ["FileUrl"] = msg.IsDeleted ? "" : (msg.FileUrl ?? ""),
                    ["FileType"] = msg.IsDeleted ? "" : (msg.FileType ?? ""),
                    ["Time"] = msg.Timestamp.ToLocalTime().ToString("HH:mm"),
                    ["IsAdmin"] = msg.User == "#Админ",
                    ["IsDeleted"] = msg.IsDeleted,
                    ["PinnedBy"] = pinnedMessage.PinnedBy,
                    ["PinnedAt"] = pinnedMessage.PinnedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                };
                await Clients.Caller.SendAsync("PinnedMessage", pinnedData);
            }
        }
        
        var messages = await _db.Messages.OrderByDescending(m => m.Timestamp).Take(100).ToListAsync();
        messages.Reverse();
        var normalizedMessages = messages.Select(m => {
            string userNick = m.User.StartsWith('#') ? m.User.Substring(1) : m.User;
            return new Dictionary<string, object> {
                ["Id"] = m.Id,
                ["Nickname"] = userNick,
                ["Text"] = m.IsDeleted ? "Сообщение удалено" : m.Text,
                ["FileUrl"] = m.IsDeleted ? "" : (m.FileUrl ?? ""),
                ["FileType"] = m.IsDeleted ? "" : (m.FileType ?? ""),
                ["Time"] = m.Timestamp.ToLocalTime().ToString("HH:mm"),
                ["IsAdmin"] = m.User == "#Админ",
                ["IsDeleted"] = m.IsDeleted
            };
        }).ToList();
        await Clients.Caller.SendAsync("LoadHistory", normalizedMessages);
        
        var loginData = new Dictionary<string, object>
        {
            ["Nickname"] = displayNick,
            ["SessionToken"] = user.SessionToken,
            ["IsAdmin"] = user.IsAdmin
        };
        
        await Clients.Caller.SendAsync("LoginSuccess", loginData);
        await UpdateOnlineUsers();
    }

    public async Task SendMessage(string text)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ConnectionId == Context.ConnectionId);
        if (user == null) return;

        if (user.IsAdmin && text.StartsWith("/create ")) { await CreateUser(text); return; }
        if (user.IsAdmin && text.StartsWith("/approve ")) { await ApproveUser(text); return; }

        var message = new ChatMessage { User = user.Nickname, Text = text, Timestamp = DateTime.UtcNow };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        string displayNick = user.Nickname.StartsWith('#') ? user.Nickname.Substring(1) : user.Nickname;
        var msgData = new Dictionary<string, object> {
            ["Id"] = message.Id,
            ["Nickname"] = displayNick,
            ["Text"] = text,
            ["FileUrl"] = "",
            ["FileType"] = "",
            ["Time"] = DateTime.Now.ToString("HH:mm"),
            ["IsAdmin"] = user.IsAdmin,
            ["IsDeleted"] = false
        };
        await Clients.All.SendAsync("ReceiveMessage", msgData);
    }

    public async Task PinMessage(int messageId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ConnectionId == Context.ConnectionId);
        if (user == null || !user.IsAdmin)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", new Dictionary<string, object> {
                ["Nickname"] = "Система",
                ["Text"] = "❌ Только админ может закреплять сообщения",
                ["Time"] = DateTime.Now.ToString("HH:mm"),
                ["IsAdmin"] = false,
                ["IsDeleted"] = false
            });
            return;
        }

        var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null) return;

        // Удаляем старое закреплённое
        var oldPinned = await _db.PinnedMessages.FirstOrDefaultAsync();
        if (oldPinned != null)
        {
            var oldMsg = await _db.Messages.FirstOrDefaultAsync(m => m.Id == oldPinned.MessageId);
            if (oldMsg != null)
            {
                oldMsg.IsPinned = false;
            }
            _db.PinnedMessages.Remove(oldPinned);
        }

        // Закрепляем новое
        _db.PinnedMessages.Add(new PinnedMessage {
            MessageId = messageId,
            PinnedBy = user.Nickname,
            PinnedAt = DateTime.UtcNow
        });
        message.IsPinned = true;
        await _db.SaveChangesAsync();

        // Отправляем всем обновлённое закреплённое
        string displayNick = message.User.StartsWith('#') ? message.User.Substring(1) : message.User;
        var pinnedData = new Dictionary<string, object> {
            ["Id"] = message.Id,
            ["Nickname"] = displayNick,
            ["Text"] = message.IsDeleted ? "Сообщение удалено" : message.Text,
            ["FileUrl"] = message.IsDeleted ? "" : (message.FileUrl ?? ""),
            ["FileType"] = message.IsDeleted ? "" : (message.FileType ?? ""),
            ["Time"] = message.Timestamp.ToLocalTime().ToString("HH:mm"),
            ["IsAdmin"] = message.User == "#Админ",
            ["IsDeleted"] = message.IsDeleted,
            ["PinnedBy"] = user.Nickname.StartsWith('#') ? user.Nickname.Substring(1) : user.Nickname,
            ["PinnedAt"] = DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
        };
        await Clients.All.SendAsync("PinnedMessage", pinnedData);
        
        _logger.LogInformation($"[{user.Nickname}] закрепил сообщение #{messageId}");
    }

    public async Task UnpinMessage()
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ConnectionId == Context.ConnectionId);
        if (user == null || !user.IsAdmin) return;

        var pinned = await _db.PinnedMessages.FirstOrDefaultAsync();
        if (pinned != null)
        {
            var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == pinned.MessageId);
            if (message != null)
            {
                message.IsPinned = false;
            }
            _db.PinnedMessages.Remove(pinned);
            await _db.SaveChangesAsync();
            
            await Clients.All.SendAsync("MessageUnpinned");
            _logger.LogInformation($"[{user.Nickname}] открепил сообщение");
        }
    }

    public async Task DeleteMessage(int messageId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ConnectionId == Context.ConnectionId);
        if (user == null) return;

        var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null) return;

        if (message.User != user.Nickname)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", new Dictionary<string, object> {
                ["Nickname"] = "Система",
                ["Text"] = "❌ Можно удалить только своё сообщение",
                ["Time"] = DateTime.Now.ToString("HH:mm"),
                ["IsAdmin"] = false,
                ["IsDeleted"] = false
            });
            return;
        }

        // Если сообщение закреплено - открепляем
        var pinned = await _db.PinnedMessages.FirstOrDefaultAsync(p => p.MessageId == messageId);
        if (pinned != null)
        {
            _db.PinnedMessages.Remove(pinned);
            message.IsPinned = false;
            await _db.SaveChangesAsync();
            await Clients.All.SendAsync("MessageUnpinned");
        }

        message.IsDeleted = true;
        message.Text = "";
        message.FileUrl = null;
        message.FileType = null;
        await _db.SaveChangesAsync();

        var msgData = new Dictionary<string, object> {
            ["Id"] = message.Id,
            ["Nickname"] = message.User.StartsWith('#') ? message.User.Substring(1) : message.User,
            ["Text"] = "Сообщение удалено",
            ["FileUrl"] = "",
            ["FileType"] = "",
            ["Time"] = message.Timestamp.ToLocalTime().ToString("HH:mm"),
            ["IsAdmin"] = message.User == "#Админ",
            ["IsDeleted"] = true
        };
        await Clients.All.SendAsync("MessageDeleted", msgData);
    }

    public async Task SendTyping(string nickname)
    {
        await Clients.Others.SendAsync("UserTyping", nickname);
    }

    private async Task CreateUser(string command)
    {
        var parts = command.Substring(8).Split(' ');
        if (parts.Length < 2) { await Clients.Caller.SendAsync("ReceiveMessage", new Dictionary<string, object> { ["Nickname"] = "Система", ["Text"] = "❌ Использование: /create #Ник Пароль", ["Time"] = DateTime.Now.ToString("HH:mm"), ["IsAdmin"] = false }); return; }
        var nick = parts[0].Trim(); if (!nick.StartsWith('#')) nick = "#" + nick;
        
        if (await _db.Users.AnyAsync(u => u.Nickname == nick)) {
            await Clients.Caller.SendAsync("ReceiveMessage", new Dictionary<string, object> { ["Nickname"] = "Система", ["Text"] = $"❌ {nick} уже существует", ["Time"] = DateTime.Now.ToString("HH:mm"), ["IsAdmin"] = false }); return; 
        }

        _db.Users.Add(new User { Nickname = nick, PasswordHash = BCrypt.Net.BCrypt.HashPassword(parts[1].Trim()), IsApproved = true, IsAdmin = false });
        await _db.SaveChangesAsync();
        await Clients.Caller.SendAsync("ReceiveMessage", new Dictionary<string, object> { ["Nickname"] = "Система", ["Text"] = $"✅ {nick} создан!", ["Time"] = DateTime.Now.ToString("HH:mm"), ["IsAdmin"] = false });
    }

    private async Task ApproveUser(string command)
    {
        var nick = command.Substring(9).Trim(); if (!nick.StartsWith('#')) nick = "#" + nick;
        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Nickname == nick);
        if (targetUser != null) {
            targetUser.IsApproved = true; await _db.SaveChangesAsync();
            await Clients.Caller.SendAsync("ReceiveMessage", new Dictionary<string, object> { ["Nickname"] = "Система", ["Text"] = $"✅ {nick} одобрен!", ["Time"] = DateTime.Now.ToString("HH:mm"), ["IsAdmin"] = false });
        }
    }

    public async Task SendFile(string fileName, string fileUrl, string fileType)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ConnectionId == Context.ConnectionId);
        if (user == null) return;

        var message = new ChatMessage { User = user.Nickname, Text = fileName, FileUrl = fileUrl, FileType = fileType, Timestamp = DateTime.UtcNow };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        string displayNick = user.Nickname.StartsWith('#') ? user.Nickname.Substring(1) : user.Nickname;
        await Clients.All.SendAsync("ReceiveMessage", new Dictionary<string, object> {
            ["Id"] = message.Id,
            ["Nickname"] = displayNick,
            ["Text"] = fileName,
            ["FileUrl"] = fileUrl,
            ["FileType"] = fileType,
            ["Time"] = DateTime.Now.ToString("HH:mm"),
            ["IsAdmin"] = user.IsAdmin,
            ["IsDeleted"] = false
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ConnectionId == Context.ConnectionId);
        if (user != null) {
            user.IsOnline = false; await _db.SaveChangesAsync();
            string displayNick = user.Nickname.StartsWith('#') ? user.Nickname.Substring(1) : user.Nickname;
            await Clients.All.SendAsync("UserLeft", displayNick);
            await UpdateOnlineUsers();
        }
        await base.OnDisconnectedAsync(exception);
    }

    private async Task UpdateOnlineUsers()
    {
        var onlineUsers = await _db.Users.Where(u => u.IsOnline).ToListAsync();
        var displayNames = onlineUsers.Select(u => u.Nickname.StartsWith('#') ? u.Nickname.Substring(1) : u.Nickname).ToList();
        await Clients.All.SendAsync("UpdateUsers", displayNames);
    }
}
