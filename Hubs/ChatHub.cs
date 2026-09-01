using FamilyChat.Constants;
using FamilyChat.DTO;
using FamilyChat.Interfaces;
using FamilyChat.Models;
using FamilyChat.Services;
using Microsoft.AspNetCore.SignalR;

namespace FamilyChat.Hubs;

public class ChatHub : Hub
{
    public const string FamilyGroup = "family";

    private readonly IChatService _chatService;
    private readonly IUserService _userService;
    private readonly IMessageService _messageService;
    private readonly PresenceTracker _presence;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatService chatService,
        IUserService userService,
        IMessageService messageService,
        PresenceTracker presence,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _userService = userService;
        _messageService = messageService;
        _presence = presence;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _userService.SetUserOfflineAsync(Context.ConnectionId);
        await Clients.All.SendAsync("UpdateOnlineUsers", await _userService.GetUsersAsync());
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Logout()
    {
        await _userService.InvalidateSessionAsync(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, FamilyGroup);
        await Clients.All.SendAsync("UpdateOnlineUsers", await _userService.GetUsersAsync());
    }

    public async Task<LoginResultDto> RegisterOrLogin(string nickname, string password)
    {
        try
        {
            var (user, isNew) = await _userService.RegisterOrLoginAsync(nickname, password);
            await CompleteLoginAsync(user);
            return OkLogin(user, isNew);
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

    public async Task SendMessage(string text)
    {
        var user = await RequireUserAsync();
        var check = InputValidator.ValidateMessage(text);
        if (!check.ok)
            throw new HubException(check.error);

        var message = await _chatService.SaveMessageAsync(user.Nickname, text);
        await Clients.Group(FamilyGroup).SendAsync("ReceiveMessage", MapMessage(message, user.IsAdmin));
    }

    public async Task SendFile(string text, string fileUrl, string fileType)
    {
        var user = await RequireUserAsync();
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new HubException("Нет файла");

        var message = await _chatService.SaveMessageAsync(user.Nickname, text ?? "", fileUrl, fileType);
        await Clients.Group(FamilyGroup).SendAsync("ReceiveMessage", MapMessage(message, user.IsAdmin));
    }

    public async Task PinMessage(int messageId)
    {
        var user = await RequireUserAsync();
        if (!user.IsAdmin) throw new HubException("Нет прав");

        await _chatService.PinMessageAsync(messageId, user.Nickname);
        var pinned = await _chatService.GetLastPinnedAsync();
        if (pinned?.Message != null)
            await Clients.Group(FamilyGroup).SendAsync("PinnedMessage", MapPinned(pinned));
    }

    public async Task UnpinMessage()
    {
        var user = await RequireUserAsync();
        if (!user.IsAdmin) throw new HubException("Нет прав");

        await _chatService.UnpinLastAsync();
        await Clients.Group(FamilyGroup).SendAsync("MessageUnpinned");
    }

    public async Task DeleteMessage(int messageId)
    {
        var user = await RequireUserAsync();
        var msg = await _chatService.FindMessageByIdAsync(messageId);
        if (msg != null && (msg.User == user.Nickname || user.IsAdmin))
        {
            await _chatService.MarkDeletedAsync(messageId);
            await Clients.Group(FamilyGroup).SendAsync("MessageDeleted", messageId);
        }
    }

    public async Task SearchMessages(string query)
    {
        await RequireUserAsync();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return;
        var results = await _chatService.SearchMessagesAsync(query, 50);
        await Clients.Caller.SendAsync("SearchResults", results.Select(m => MapMessage(m)).ToList());
    }

    public async Task SendPrivateMessage(string receiverNickname, string text)
    {
        var sender = await RequireUserAsync();
        var receiver = await _userService.FindByNicknameAsync(receiverNickname)
            ?? throw new HubException("Пользователь не найден");

        var check = InputValidator.ValidateMessage(text);
        if (!check.ok) throw new HubException(check.error);

        var saved = await _messageService.SavePrivateMessageAsync(sender.Nickname, receiver.Nickname, text);
        var payload = MapPrivate(saved);
        await Clients.Group(PresenceTracker.UserGroup(receiver.Nickname)).SendAsync("ReceivePrivateMessage", payload);
        await Clients.Caller.SendAsync("ReceivePrivateMessage", payload);
    }

    public async Task SendPrivateFile(string receiverNickname, string text, string fileUrl, string fileType)
    {
        var sender = await RequireUserAsync();
        var receiver = await _userService.FindByNicknameAsync(receiverNickname)
            ?? throw new HubException("Пользователь не найден");

        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new HubException("Нет файла");

        var saved = await _messageService.SavePrivateMessageAsync(sender.Nickname, receiver.Nickname, text ?? "", fileUrl, fileType);
        var payload = MapPrivate(saved);
        await Clients.Group(PresenceTracker.UserGroup(receiver.Nickname)).SendAsync("ReceivePrivateMessage", payload);
        await Clients.Caller.SendAsync("ReceivePrivateMessage", payload);
    }

    public async Task LoadPrivateHistory(string otherNickname)
    {
        var me = await RequireUserAsync();
        var other = await _userService.FindByNicknameAsync(otherNickname);
        if (other == null) return;

        var history = await _messageService.GetPrivateMessagesAsync(me.Nickname, other.Nickname);
        await Clients.Caller.SendAsync("LoadPrivateHistory", history.Select(m => MapPrivate(m)).ToList());
    }

    public async Task LoadFamilyHistory()
    {
        await RequireUserAsync();
        var history = await _chatService.GetRecentMessagesAsync(AppConstants.HistoryLimit);
        await Clients.Caller.SendAsync("LoadHistory", history.Select(m => MapMessage(m)).ToList());
    }

    private async Task CompleteLoginAsync(User user)
    {
        await _userService.SetUserOnlineAsync(Context.ConnectionId, user.Nickname);
        _presence.Connect(Context.ConnectionId, user.Nickname);

        await Groups.AddToGroupAsync(Context.ConnectionId, FamilyGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, PresenceTracker.UserGroup(user.Nickname));

        var history = await _chatService.GetRecentMessagesAsync(AppConstants.HistoryLimit);
        var pinned = await _chatService.GetLastPinnedAsync();
        var users = await _userService.GetUsersAsync();

        await Clients.Caller.SendAsync("LoadHistory", history.Select(m => MapMessage(m)).ToList());

        if (pinned?.Message != null)
            await Clients.Caller.SendAsync("PinnedMessage", MapPinned(pinned));

        await Clients.All.SendAsync("UpdateOnlineUsers", users);
    }

    private async Task<User> RequireUserAsync()
    {
        var user = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        if (user != null) return user;

        var nick = _presence.NicknameOf(Context.ConnectionId);
        if (!string.IsNullOrEmpty(nick))
            user = await _userService.FindByNicknameAsync(nick);

        if (user == null)
            throw new HubException("Сначала войдите в чат");

        return user;
    }

    private static LoginResultDto OkLogin(User user, bool isNew = false) => new()
    {
        Success = true,
        Nickname = user.Nickname,
        SessionToken = user.SessionToken,
        IsAdmin = user.IsAdmin,
        IsNew = isNew
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

    private static object MapPrivate(PrivateMessage message) => new
    {
        id = message.Id,
        sender = message.SenderId,
        receiver = message.ReceiverId,
        text = message.Text,
        fileUrl = message.FileUrl,
        fileType = message.FileType,
        time = message.Timestamp.ToString("HH:mm"),
        timestamp = message.Timestamp
    };
}
