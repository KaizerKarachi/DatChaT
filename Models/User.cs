using System.ComponentModel.DataAnnotations;

namespace FamilyChat.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Nickname { get; set; } = "";
    
    [Required]
    public string PasswordHash { get; set; } = "";

    public string? SessionToken { get; set; }
    
    public bool IsApproved { get; set; } = false;
    public bool IsAdmin { get; set; } = false;
    public string? ConnectionId { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; }
}
