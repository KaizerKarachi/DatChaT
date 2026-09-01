using FamilyChat.Models;
namespace FamilyChat.Interfaces;
public interface IMessageService
{
    Task<PrivateMessage> SavePrivateMessageAsync(string senderId, string receiverId, string text, string? fileUrl = null, string? fileType = null);
    Task<List<PrivateMessage>> GetPrivateMessagesAsync(string userId1, string userId2);
}
