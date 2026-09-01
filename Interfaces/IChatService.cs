using FamilyChat.Constants;
using FamilyChat.Models;
namespace FamilyChat.Interfaces;
public interface IChatService
{
    Task<ChatMessage?> FindMessageByIdAsync(int messageId);
    Task<List<ChatMessage>> GetRecentMessagesAsync(int count = AppConstants.HistoryLimit);
    Task<ChatMessage> SaveMessageAsync(string userId, string text, string? fileUrl = null, string? fileType = null);
    Task MarkDeletedAsync(int messageId);
    Task<List<ChatMessage>> SearchMessagesAsync(string searchText, int limit = 50);
    Task<PinnedMessage?> GetLastPinnedAsync();
    Task PinMessageAsync(int messageId, string userId);
    Task UnpinLastAsync();
}
