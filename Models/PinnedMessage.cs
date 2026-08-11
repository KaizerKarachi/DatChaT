using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyChat.Models;

[Index(nameof(MessageId))]
[Index(nameof(PinnedBy))]
[Index(nameof(PinnedAt))]
public class PinnedMessage
{
    [Key]
    public int Id { get; set; }

    public int MessageId { get; set; }
    
    // !!! ВОТ ЭТОГО НЕ ХВАТАЛО !!!
    // Навигационное свойство для Entity Framework Core
    public ChatMessage? Message { get; set; }

    public string PinnedBy { get; set; } = "";
    public DateTime PinnedAt { get; set; }
}
