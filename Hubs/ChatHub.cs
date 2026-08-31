using FamilyChat.Constants;
using FamilyChat.DTO;
using FamilyChat.Interfaces;
using FamilyChat.Models;
using FamilyChat.Services;
using Microsoft.AspNetCore.SignalR;

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
        await Clients.All.SendAsync("UpdateOnlineUsers", await _userService.GetUsersAsync());
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<LoginResultDto> RegisterOrLogin(string nickname, string password)
    {
        try
        {
            var user = await _userService.RegisterOrLoginAsync(nickname, password);
            await CompleteLoginAsync(user);
            return OkLogin(user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка входа: {Nickname}", nickname);
            return new LoginResultDto { Success = false, Error = ex.Message };
        }
    }

    public async Task<LoginResultDto> JoinByToken(string nickname, string sessionToken)
    {
        try
        {
            var user = await _userService.JoinByTokenAsync(nickname, sessionToken);
            await CompleteLoginAsync(user);
            return OkLogin(user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка входа по токену: {Nickname}", nickname);
            return new LoginResultDto { Success = false, Error = ex.Message };
        }
    }

    public async Task SendMessage(string text, string? fileUrl = null, string? fileType = null)
    {
        var user = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        if (user == null) return;

        var hasFile = !string.IsNullOrWhiteSpace(fileUrl);
        if (!hasFile)
        {
            var check = InputValidator.ValidateMessage(text);
            if (!check.ok) return;
        }

        var message = await _chatService.SaveMessageAsync(user.Nickname, text ?? "", fileUrl, fileType);
        await Clients.All.SendAsync("ReceiveMessage", MapMessage(message, user.IsAdmin));
    }

    public async Task PinMessage(int messageId)
    {
        var user = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        if (user == null || !user.IsAdmin) return;

        await _chatService.PinMessageAsync(messageId, user.Nickname);
        var pinned = await _chatService.GetLastPinnedAsync();
        if (pinned?.Message != null)
            await Clients.All.SendAsync("PinnedMessage", MapPinned(pinned));
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
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return;
        var results = await _chatService.SearchMessagesAsync(query, 50);
        await Clients.Caller.SendAsync("SearchResults", results.Select(m => MapMessage(m)).ToList());
    }

    public async Task SendPrivateMessage(string receiverNickname, string text, string? fileUrl = null, string? fileType = null)
    {
        var sender = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        var receiver = await _userService.FindByNicknameAsync(receiverNickname);

        if (sender == null || receiver == null)
        {
            await Clients.Caller.SendAsync("SystemMessage", "Пользователь не найден");
            return;
        }

        var hasFile = !string.IsNullOrWhiteSpace(fileUrl);
        if (!hasFile)
        {
            var check = InputValidator.ValidateMessage(text);
            if (!check.ok) return;
        }

        var saved = await _messageService.SavePrivateMessageAsync(sender.Nickname, receiver.Nickname, text ?? "");
        var payload = MapPrivate(saved, fileUrl, fileType);

        if (!string.IsNullOrEmpty(receiver.ConnectionId))
            await Clients.Client(receiver.ConnectionId).SendAsync("ReceivePrivateMessage", payload);

        await Clients.Caller.SendAsync("ReceivePrivateMessage", payload);
    }

    public async Task LoadPrivateHistory(string otherNickname)
    {
        var me = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        var other = await _userService.FindByNicknameAsync(otherNickname);
        if (me == null || other == null) return;

        var history = await _messageService.GetPrivateMessagesAsync(me.Nickname, other.Nickname);
        await Clients.Caller.SendAsync("LoadPrivateHistory", history.Select(m => MapPrivate(m)).ToList());
    }

    public async Task LoadFamilyHistory()
    {
        var history = await _chatService.GetRecentMessagesAsync(AppConstants.HistoryLimit);
        await Clients.Caller.SendAsync("LoadHistory", history.Select(m => MapMessage(m)).ToList());
    }

    private async Task CompleteLoginAsync(User user)
    {
        await _userService.SetUserOnlineAsync(Context.ConnectionId, user.Nickname);

        var history = await _chatService.GetRecentMessagesAsync(AppConstants.HistoryLimit);
        var pinned = await _chatService.GetLastPinnedAsync();
        var users = await _userService.GetUsersAsync();

        await Clients.Caller.SendAsync("LoadHistory", history.Select(m => MapMessage(m)).ToList());

        if (pinned?.Message != null)
            await Clients.Caller.SendAsync("PinnedMessage", MapPinned(pinned));

        await Clients.All.SendAsync("UpdateOnlineUsers", users);
    }

    private static LoginResultDto OkLogin(User user) => new()
    {
        Success = true,
        Nickname = user.Nickname,
        SessionToken = user.SessionToken,
        IsAdmin = user.IsAdmin
    };

    private static object MapMessage(ChatMessage message, bool isAdmin = false) => new
    {
        id = message.Id,
        nickname = message.User,
        text = message.Text,
        fileUrl = message.FileUrl,
        fileType = message.FileType,
        time = message.Timestamp.ToString("HH:mm"),
        timestamp = message.Timestamp,
        isAdmin,
        isDeleted = message.IsDeleted,
        isPinned = message.IsPinned
    };

    private static object MapPinned(PinnedMessage pinned) => new
    {
        id = pinned.MessageId,
        nickname = pinned.Message!.User,
        text = pinned.Message.Text,
        fileUrl = pinned.Message.FileUrl,
        fileType = pinned.Message.FileType,
        time = pinned.Message.Timestamp.ToString("HH:mm"),
        isDeleted = pinned.Message.IsDeleted,
        pinnedBy = pinned.PinnedBy,
        pinnedAt = pinned.PinnedAt.ToString("HH:mm dd.MM.yyyy")
    };

    private static object MapPrivate(PrivateMessage message, string? fileUrl = null, string? fileType = null) => new
    {
        id = message.Id,
        sender = message.SenderId,
        receiver = message.ReceiverId,
        text = message.Text,
        fileUrl,
        fileType,
        time = message.Timestamp.ToString("HH:mm"),
        timestamp = message.Timestamp
    };
}
