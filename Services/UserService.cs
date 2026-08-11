using System.Linq;
using System.Threading.Tasks;
using FamilyChat.Data;
using FamilyChat.Interfaces;
using FamilyChat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyChat.Services;

public class UserService : IUserService
{
    private readonly ChatDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(ChatDbContext db, ILogger<UserService> logger) => (_db, _logger) = (db, logger);

    private static string NormalizeNickname(string nickname) => 
        string.IsNullOrWhiteSpace(nickname) ? nickname : (nickname.StartsWith('#') ? nickname : "#" + nickname);

    public Task<User?> FindByConnectionIdAsync(string connectionId) => 
        _db.Users.FirstOrDefaultAsync(u => u.ConnectionId == connectionId);

    public Task<User?> FindByNicknameAsync(string nickname) => 
        _db.Users.FirstOrDefaultAsync(u => u.Nickname == NormalizeNickname(nickname));

    public async Task<User> RegisterOrLoginAsync(string nickname, string password)
    {
        var normalizedNick = NormalizeNickname(nickname);
        var user = await FindByNicknameAsync(normalizedNick);

        if (user == null)
        {
            user = new User 
            { 
                Nickname = normalizedNick, 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), 
                IsApproved = true, 
                SessionToken = Guid.NewGuid().ToString() 
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            _logger.LogInformation("✅ [AUTH] Зарегистрирован новый пользователь: {Nickname}", normalizedNick);
        }
        else if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("⚠️ [AUTH] Неверный пароль для пользователя: {Nickname}", normalizedNick);
            throw new UnauthorizedAccessException("Неверный ник или пароль");
        }
        else
        {
            user.SessionToken = Guid.NewGuid().ToString();
            await _db.SaveChangesAsync();
            _logger.LogInformation("✅ [AUTH] Успешный вход пользователя: {Nickname}", normalizedNick);
        }

        return user;
    }

    public async Task<User> JoinByTokenAsync(string nickname, string sessionToken)
    {
        var normalizedNick = NormalizeNickname(nickname);
        var user = await FindByNicknameAsync(normalizedNick);
        
        if (user == null || user.SessionToken != sessionToken)
        {
            _logger.LogWarning("⚠️ [AUTH] Попытка входа по недействительному токену: {Nickname}", normalizedNick);
            throw new UnauthorizedAccessException("Сессия истекла или недействительна");
        }

        _logger.LogInformation("✅ [AUTH] Успешный вход по токену: {Nickname}", normalizedNick);
        return user;
    }

    public async Task SetUserOnlineAsync(string connectionId, string nickname)
    {
        var user = await FindByNicknameAsync(nickname);
        if (user != null)
        {
            user.ConnectionId = connectionId;
            user.IsOnline = true;
            user.LastSeen = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task SetUserOfflineAsync(string connectionId)
    {
        var user = await FindByConnectionIdAsync(connectionId);
        if (user != null)
        {
            user.IsOnline = false;
            user.ConnectionId = null;
            user.LastSeen = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetOnlineUsersAsync() => 
        (await _db.Users.Where(u => u.IsOnline).Select(u => u.Nickname).ToListAsync())
            .Select(n => n.StartsWith('#') ? n[1..] : n).ToList();
}
