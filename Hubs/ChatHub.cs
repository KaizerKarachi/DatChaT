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
    private readonly ITdApiService _td;
    private readonly PresenceTracker _presence;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatService chatService,
        IUserService userService,
        ITdApiService td,
        PresenceTracker presence,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _userService = userService;
        _td = td;
        _presence = presence;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _userService.SetUserOfflineAsync(Context.ConnectionId);
        await BroadcastDirectory();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Logout()
    {
        await _userService.InvalidateSessionAsync(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, FamilyGroup);
        await BroadcastDirectory();
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

    public async Task<List<TdChatDto>> GetChats()
    {
        var user = await RequireUserAsync();
        return await _td.GetChatsAsync(user);
    }

    public async Task GetChatHistory(string chatId)
    {
        var user = await TryUserAsync();
        if (user == null) return;
        var history = await _td.GetChatHistoryAsync(user, chatId);
        await Clients.Caller.SendAsync("updateChatHistory", history);
    }

    public async Task ViewMessages(string chatId)
    {
        var user = await TryUserAsync();
        if (user == null) return;
        await _td.ViewMessagesAsync(user, chatId);
        await Clients.Caller.SendAsync("updateChatReadInbox", new { chatId, unreadCount = 0 });
    }

    public async Task SendChatMessage(string chatId, string text)
    {
        var user = await RequireUserAsync();
        var message = await _td.SendTextAsync(user, chatId, text);
        await _td.IncrementUnreadAsync(chatId, user.Nickname);
        await BroadcastNewMessage(chatId, message);
    }

    public async Task SendChatFile(string chatId, string text, string fileUrl, string fileType)
    {
        var user = await RequireUserAsync();
        var message = await _td.SendFileAsync(user, chatId, text, fileUrl, fileType);
        await _td.IncrementUnreadAsync(chatId, user.Nickname);
        await BroadcastNewMessage(chatId, message);
    }

    public async Task SetChatAction(string chatId, string action)
    {
        var user = await RequireUserAsync();
        var payload = new { chatId, userId = user.Nickname, action };
        if (ChatIds.IsFamily(chatId))
            await Clients.OthersInGroup(FamilyGroup).SendAsync("updateChatAction", payload);
        else
        {
            var peer = ChatIds.PeerNickname(chatId);
            if (!string.IsNullOrEmpty(peer))
                await Clients.Group(PresenceTracker.UserGroup(peer)).SendAsync("updateChatAction", payload);
        }
    }

    public Task SendMessage(string text) => SendChatMessage(ChatIds.Family, text);

    public Task SendFile(string text, string fileUrl, string fileType) =>
        SendChatFile(ChatIds.Family, text, fileUrl, fileType);

    public Task SendPrivateMessage(string receiverNickname, string text) =>
        SendChatMessage(ChatIds.Private(UserService.NormalizeNickname(receiverNickname)), text);

    public Task SendPrivateFile(string receiverNickname, string text, string fileUrl, string fileType) =>
        SendChatFile(ChatIds.Private(UserService.NormalizeNickname(receiverNickname)), text, fileUrl, fileType);

    public Task LoadPrivateHistory(string otherNickname) =>
        GetChatHistory(ChatIds.Private(UserService.NormalizeNickname(otherNickname)));

    public Task LoadFamilyHistory() => GetChatHistory(ChatIds.Family);

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
            await Clients.Group(FamilyGroup).SendAsync("updateDeleteMessages", new { chatId = ChatIds.Family, messageIds = new[] { "f-" + messageId } });
            await Clients.Group(FamilyGroup).SendAsync("MessageDeleted", messageId);
        }
    }

    public async Task SearchMessages(string query)
    {
        await RequireUserAsync();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return;
        var results = await _chatService.SearchMessagesAsync(query, 50);
        await Clients.Caller.SendAsync("SearchResults", results.Select(MapMessage).ToList());
    }

    private async Task BroadcastNewMessage(string chatId, TdMessageDto message)
    {
        var envelope = new { chatId, message };
        await Clients.Caller.SendAsync("updateNewMessage", envelope);
        await Clients.Caller.SendAsync("updateChatLastMessage", new { chatId, lastMessage = message });

        if (ChatIds.IsFamily(chatId))
        {
            await Clients.OthersInGroup(FamilyGroup).SendAsync("updateNewMessage", envelope);
            await Clients.OthersInGroup(FamilyGroup).SendAsync("updateChatLastMessage", new { chatId, lastMessage = message });
            return;
        }

        var peer = ChatIds.PeerNickname(chatId);
        if (string.IsNullOrEmpty(peer)) return;

        var peerChatId = ChatIds.Private(message.SenderId);
        await Clients.Group(PresenceTracker.UserGroup(peer)).SendAsync("updateNewMessage", new { chatId = peerChatId, message });
        await Clients.Group(PresenceTracker.UserGroup(peer)).SendAsync("updateChatLastMessage", new { chatId = peerChatId, lastMessage = message });
    }

    private async Task BroadcastDirectory()
    {
        var users = await _userService.GetUsersAsync();
        await Clients.All.SendAsync("updateUsers", users);
        await Clients.All.SendAsync("UpdateOnlineUsers", users);
    }

    private async Task CompleteLoginAsync(User user)
    {
        await _userService.SetUserOnlineAsync(Context.ConnectionId, user.Nickname);

        await Groups.AddToGroupAsync(Context.ConnectionId, FamilyGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, PresenceTracker.UserGroup(user.Nickname));

        var chats = await _td.GetChatsAsync(user);
        var pinned = await _chatService.GetLastPinnedAsync();

        await Clients.Caller.SendAsync("updateAuthorizationState", new { type = "ready" });
        await Clients.Caller.SendAsync("updateChats", chats);
        if (pinned?.Message != null)
            await Clients.Caller.SendAsync("PinnedMessage", MapPinned(pinned));

        await BroadcastDirectory();
    }

    private async Task<User?> TryUserAsync()
    {
        var user = await _userService.FindByConnectionIdAsync(Context.ConnectionId);
        if (user != null) return user;

        var nick = _presence.NicknameOf(Context.ConnectionId);
        if (string.IsNullOrEmpty(nick)) return null;
        return await _userService.FindByNicknameAsync(nick);
    }

    private async Task<User> RequireUserAsync()
    {
        var user = await TryUserAsync();
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

    private static object MapMessage(ChatMessage message) => new
    {
        id = message.Id,
        nickname = message.User,
        text = message.Text,
        fileUrl = message.FileUrl,
        fileType = message.FileType,
        time = message.Timestamp.ToString("HH:mm"),
        timestamp = message.Timestamp,
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
}
