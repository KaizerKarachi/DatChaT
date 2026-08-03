using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public MessageService(ChatDbContext db, ILogger<MessageService> logger) 
    { 
        _db = db; 
        _logger = logger; 
    }

    public async Task<PrivateMessage> SavePrivateMessageAsync(string senderId, string receiverId, string text) 
    {
        var pm = new PrivateMessage 
        { 
            SenderId = senderId, 
            ReceiverId = receiverId, 
            Text = text, 
            Timestamp = DateTime.UtcNow, 
            IsRead = false 
        };
        
        _db.PrivateMessages.Add(pm);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("📩 [PM] Личное сообщение от {Sender} к {Receiver}", senderId, receiverId);
        return pm;
    }

    public async Task<List<PrivateMessage>> GetPrivateMessagesAsync(string userId1, string userId2) 
    {
        return await _db.PrivateMessages
            .Where(pm => (pm.SenderId == userId1 && pm.ReceiverId == userId2) || 
                         (pm.SenderId == userId2 && pm.ReceiverId == userId1))
            .OrderBy(pm => pm.Timestamp)
            .ToListAsync();
    }
}
