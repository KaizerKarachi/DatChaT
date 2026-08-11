using FamilyChat.Data;
using FamilyChat.Interfaces;
using FamilyChat.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Services;

public class ChatService : IChatService
{
    private readonly ChatDbContext _db;
    private readonly ILogger<ChatService> _logger;
    
    public ChatService(ChatDbContext db, ILogger<ChatService> logger) => (_db, _logger) = (db, logger);

    public Task<ChatMessage?> FindMessageByIdAsync(int messageId) => 
        _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(int count = 100)
    {
        var messages = await _db.Messages.AsNoTracking()
            .OrderByDescending(x => x.Timestamp).Take(count).ToListAsync();
        messages.Reverse();
        return messages;
    }

    public async Task<ChatMessage> SaveMessageAsync(string userId, string text, string? fileUrl = null, string? fileType = null)
    {
        var msg = new ChatMessage { User = userId, Text = text, FileUrl = fileUrl, FileType = fileType, Timestamp = DateTime.UtcNow };
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();
        return msg;
    }

    public async Task MarkDeletedAsync(int messageId)
    {
        var msg = await FindMessageByIdAsync(messageId);
        if (msg == null) return;

        var pinned = await _db.PinnedMessages.FirstOrDefaultAsync(x => x.MessageId == messageId);
        if (pinned != null)
        {
            _db.PinnedMessages.Remove(pinned);
            msg.IsPinned = false;
        }

        msg.IsDeleted = true;
        msg.Text = "";
        msg.FileUrl = null;
        msg.FileType = null;
        await _db.SaveChangesAsync();
    }

    public Task<List<ChatMessage>> SearchMessagesAsync(string searchText, int limit = 50) => 
        _db.Messages.AsNoTracking()
            .Where(m => m.Text.Contains(searchText) && !m.IsDeleted)
            .OrderByDescending(m => m.Timestamp).Take(limit).ToListAsync();

    public Task<PinnedMessage?> GetLastPinnedAsync() => 
        _db.PinnedMessages.Include(p => p.Message).AsNoTracking()
            .OrderByDescending(p => p.PinnedAt).FirstOrDefaultAsync();

    public async Task PinMessageAsync(int messageId, string userId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var old = await _db.PinnedMessages.Include(p => p.Message)
                .OrderByDescending(p => p.PinnedAt).FirstOrDefaultAsync();
            
            if (old?.Message != null)
            {
                old.Message.IsPinned = false;
                _db.PinnedMessages.Remove(old);
            }

            var msg = await FindMessageByIdAsync(messageId);
            if (msg != null)
            {
                _db.PinnedMessages.Add(new PinnedMessage { MessageId = messageId, PinnedBy = userId, PinnedAt = DateTime.UtcNow });
                msg.IsPinned = true;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task UnpinLastAsync()
    {
        var pinned = await _db.PinnedMessages.Include(p => p.Message)
            .OrderByDescending(x => x.PinnedAt).FirstOrDefaultAsync();
        
        if (pinned?.Message != null)
        {
            pinned.Message.IsPinned = false;
            _db.PinnedMessages.Remove(pinned);
            await _db.SaveChangesAsync();
        }
    }
}
