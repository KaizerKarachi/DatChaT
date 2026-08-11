using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyChat.Models;

[Index(nameof(Timestamp))]
[Index(nameof(IsDeleted))]
[Index(nameof(User))]
[Index(nameof(IsPinned))]
public class ChatMessage
{
    [Key]
    public int Id { get; set; }
    public string User { get; set; } = "";
    public string Text { get; set; } = "";
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public bool IsPinned { get; set; } = false;
}
