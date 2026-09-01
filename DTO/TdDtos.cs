namespace FamilyChat.DTO;

public static class ChatIds
{
    public const string Family = "family";

    public static bool IsFamily(string? chatId) =>
        string.Equals(chatId, Family, StringComparison.OrdinalIgnoreCase);

    public static string Private(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            throw new ArgumentException("Пустой чат");
        var nick = nickname.StartsWith('#') ? nickname : "#" + nickname;
        return "u:" + nick;
    }

    public static string? PeerNickname(string chatId) =>
        chatId.StartsWith("u:", StringComparison.Ordinal) ? chatId[2..] : null;
}

public class TdMessageDto
{
    public string Id { get; set; } = "";
    public string ChatId { get; set; } = "";
    public string SenderId { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string Text { get; set; } = "";
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public string Time { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsPinned { get; set; }
}

public class TdChatDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "private";
    public string Title { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsOnline { get; set; }
    public string Status { get; set; } = "";
    public int UnreadCount { get; set; }
    public TdMessageDto? LastMessage { get; set; }
    public long Order { get; set; }
}

public class TdChatHistoryDto
{
    public string ChatId { get; set; } = "";
    public List<TdMessageDto> Messages { get; set; } = new();
}