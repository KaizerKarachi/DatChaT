namespace FamilyChat.DTO;

public class UserListItemDto
{
    public string Nickname { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsOnline { get; set; }
    public string Status { get; set; } = "";
    public DateTime LastSeen { get; set; }
}
