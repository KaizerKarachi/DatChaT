namespace FamilyChat.DTO;

public class ChatMessageDto
{
    public int Id { get; set; }

    public string Nickname { get; set; } = "";

    public string Text { get; set; } = "";

    public string FileUrl { get; set; } = "";

    public string FileType { get; set; } = "";

    public string Time { get; set; } = "";

    public bool IsAdmin { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsPinned { get; set; }

    public bool IsSearchResult { get; set; }
}
