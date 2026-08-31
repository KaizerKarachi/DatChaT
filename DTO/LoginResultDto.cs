namespace FamilyChat.DTO;

public class LoginResultDto
{
    public bool Success { get; set; }
    public string? Nickname { get; set; }
    public string? SessionToken { get; set; }
    public bool IsAdmin { get; set; }
    public string? Error { get; set; }
}
