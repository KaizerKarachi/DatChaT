using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyChat.Models;

[Index(nameof(SenderId))]
[Index(nameof(ReceiverId))]
[Index(nameof(Timestamp))]
[Index(nameof(IsRead))]
[Index(nameof(SenderId), nameof(ReceiverId), nameof(Timestamp))]
public class PrivateMessage
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string SenderId { get; set; } = "";
    
    [Required]
    public string ReceiverId { get; set; } = "";
    
    [Required]
    public string Text { get; set; } = "";
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
