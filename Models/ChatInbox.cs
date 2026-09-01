using System.ComponentModel.DataAnnotations;

namespace FamilyChat.Models;

public class ChatInbox
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string UserNickname { get; set; } = "";
    [Required]
    public string ChatId { get; set; } = "";
    public int UnreadCount { get; set; }
}