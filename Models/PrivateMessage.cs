using System;
using System.ComponentModel.DataAnnotations;

namespace FamilyChat.Models;

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
