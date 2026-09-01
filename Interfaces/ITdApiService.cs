using FamilyChat.Constants;
using FamilyChat.DTO;
using FamilyChat.Models;

namespace FamilyChat.Interfaces;

public interface ITdApiService
{
    Task<List<TdChatDto>> GetChatsAsync(User me);
    Task<TdChatHistoryDto> GetChatHistoryAsync(User me, string chatId, int limit = AppConstants.HistoryLimit);
    Task<TdMessageDto> SendTextAsync(User me, string chatId, string text);
    Task<TdMessageDto> SendFileAsync(User me, string chatId, string text, string fileUrl, string fileType);
    Task ViewMessagesAsync(User me, string chatId);
    Task IncrementUnreadAsync(string chatId, string exceptNickname);
}