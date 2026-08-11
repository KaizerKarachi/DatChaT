using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using FamilyChat.Interfaces;
using FamilyChat.Models;
using Microsoft.Extensions.Logging;

namespace FamilyChat.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IUserService _userService;
    private readonly IMessageService _messageService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, IUserService userService, IMessageService messageService, ILogger<ChatHub> logger) =>
        (_chatService, _userService, _messageService, _logger) = (chatService, userService, messageService, logger);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _userService.SetUserOfflineAsync(Context.ConnectionId);
        await Clients.All.SendAsync("UpdateOnlineUsers", await _userService.GetOnlineUsersAsync());
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<Dictionary<string, object>> RegisterOrLogin(string nickname, string password)
    {
        try
        {
            var user = await _userService.RegisterOrLoginAsync(nickname, password);
            await _userService.SetUserOnlineAsync(Context.ConnectionId, user.Nickname);
            await Clients.All.SendAsync("UpdateOnlineUsers", await _userService.GetOnlineUsersAsync());

            return new Dictionary<string, object> 
            { 
                ["success"] = true, 
                ["nickname"] = user.Nickname, 
                ["sessionToken"] = user.SessionToken!, 
                ["isAdmin"] = user.IsAdmin 
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка входа: {Nickname}", nickname);
            return new Dictionary<string, object> { ["success"] = false, ["error"] = ex.Message };
        }
    }

    public async Task JoinByToken(string nickname, string sessionToken)
    {
        try
        {
            var user = await _userService.JoinByTokenAsync(nickname, sessionToken);
            await _userService.SetUserOnlineAsync(Context.ConnectionId, user.Nickname);
            
            var history = await _chatService.GetRecentMessagesAsync(100);
            var pinned = await _chatService.GetLastPinnedAsync();
            
            await Clients.Caller.SendAsync("LoadHistory", history);
            if (pinned?.Message != null)
            {
                await Clients.Caller.SendAsync("PinnedMessage", new
                {
                    Id = pinned.MessageId,
                    Nickname = pinned.Message.User,
                    Text = pinned.Message.Text,
                    FileUrl = pinned.Message.FileUrl,
                    FileType = pinned.Message.FileType,
                    Time = pinned.Message.Timestamp.ToString("HH:mm"),
                    IsAdmin = user.IsAdmin,
                    IsDeleted = pinned.Message.IsDeleted,
                    PinnedBy = pinned.PinnedBy,
                    PinnedAt = pinned.PinnedAt.ToString("HH:mm dd.MM.yyyy")
                });
            }
            await Clients.All.SendAsync("UpdateOnlineUsers", await _userService.GetOnlineUsersAsync());
        }
        catch (Exception ex) 
        { 
            await Clients.Caller.SendAsync("SystemMessage", $"Ошибка: {ex.Message}"); 
        }
    }

    public async Task SendMessage(string text, string? fileUrl = null, string? fileType = null)
    {
        var user = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        if (user == null) return;

        var message = await _chatService.SaveMessageAsync(user.Nickname, text, fileUrl, fileType);
        await Clients.All.SendAsync("ReceiveMessage", new
        {
            Id = message.Id,
            Nickname = message.User,
            Text = message.Text,
            FileUrl = message.FileUrl,
            FileType = message.FileType,
            Time = message.Timestamp.ToString("HH:mm"),
            IsAdmin = user.IsAdmin,
            IsDeleted = message.IsDeleted,
            IsPinned = message.IsPinned
        });
    }

    public async Task PinMessage(int messageId)
    {
        var user = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        if (user == null || !user.IsAdmin) return;

        await _chatService.PinMessageAsync(messageId, user.Nickname);
        var pinned = await _chatService.GetLastPinnedAsync();
        if (pinned?.Message != null)
        {
            await Clients.All.SendAsync("PinnedMessage", new
            {
                Id = pinned.MessageId,
                Nickname = pinned.Message.User,
                Text = pinned.Message.Text,
                PinnedBy = pinned.PinnedBy,
                PinnedAt = pinned.PinnedAt.ToString("HH:mm dd.MM.yyyy")
            });
        }
    }

    public async Task UnpinMessage()
    {
        var user = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        if (user == null || !user.IsAdmin) return;

        await _chatService.UnpinLastAsync();
        await Clients.All.SendAsync("MessageUnpinned");
    }

    public async Task DeleteMessage(int messageId)
    {
        var user = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        if (user == null) return;

        var msg = await _chatService.FindMessageByIdAsync(messageId);
        if (msg != null && (msg.User == user.Nickname || user.IsAdmin))
        {
            await _chatService.MarkDeletedAsync(messageId);
            await Clients.All.SendAsync("MessageDeleted", messageId);
        }
    }

    public async Task SearchMessages(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3) return;
        var results = await _chatService.SearchMessagesAsync(query, 50);
        await Clients.Caller.SendAsync("SearchResults", results);
    }

    public async Task SendPrivateMessage(string receiverNickname, string text)
    {
        var sender = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        var receiver = await _userService.FindByNicknameAsync(receiverNickname);

        if (sender == null || receiver == null)
        {
            await Clients.Caller.SendAsync("SystemMessage", "Пользователь не найден");
            return;
        }

        await _messageService.SavePrivateMessageAsync(sender.Nickname, receiver.Nickname, text);
        if (!string.IsNullOrEmpty(receiver.ConnectionId))
            await Clients.Client(receiver.ConnectionId).SendAsync("ReceivePrivateMessage", sender.Nickname, text);
        
        await Clients.Caller.SendAsync("ReceivePrivateMessage", receiver.Nickname, text);
    }
}
