using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyChat.Models;

[Index(nameof(Nickname), IsUnique = true)]
[Index(nameof(SessionToken))]
[Index(nameof(IsApproved))]
[Index(nameof(LastSeen))]
[Index(nameof(IsOnline))]
public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Nickname { get; set; } = "";
    
    [Required]
    public string PasswordHash { get; set; } = "";
    
    // Токен сессии для автоматического входа
    public string? SessionToken { get; set; }
    
    public bool IsApproved { get; set; } = false;
    public bool IsAdmin { get; set; } = false;
    public string? ConnectionId { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; }
}
