using FamilyChat.Data;
using FamilyChat.Interfaces;
using FamilyChat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyChat.Services;

public class MessageService : IMessageService
{
    private readonly ChatDbContext _db;
    private readonly ILogger<MessageService> _logger;

    public MessageService(ChatDbContext db, ILogger<MessageService> logger) => (_db, _logger) = (db, logger);

    public async Task<PrivateMessage> SavePrivateMessageAsync(string senderId, string receiverId, string text, string? fileUrl = null, string? fileType = null)
    {
        var pm = new PrivateMessage 
        { 
            SenderId = senderId, 
            ReceiverId = receiverId, 
            Text = text, 
            Timestamp = DateTime.UtcNow, 
            IsRead = false,
            FileUrl = fileUrl,
            FileType = fileType
        };
        
        _db.PrivateMessages.Add(pm);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Личное сообщение от {Sender} к {Receiver}", senderId, receiverId);
        return pm;
    }

    public Task<List<PrivateMessage>> GetPrivateMessagesAsync(string userId1, string userId2) => 
        _db.PrivateMessages.AsNoTracking()
            .Where(pm => (pm.SenderId == userId1 && pm.ReceiverId == userId2) || 
                         (pm.SenderId == userId2 && pm.ReceiverId == userId1))
            .OrderBy(pm => pm.Timestamp)
            .ToListAsync();
}
