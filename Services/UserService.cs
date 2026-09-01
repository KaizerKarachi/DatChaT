using FamilyChat.Data;
using FamilyChat.DTO;
using FamilyChat.Interfaces;
using FamilyChat.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Services;

public class UserService : IUserService
{
    private readonly ChatDbContext _db;
    private readonly PresenceTracker _presence;
    private readonly ILogger<UserService> _logger;

    public UserService(ChatDbContext db, PresenceTracker presence, ILogger<UserService> logger) =>
        (_db, _presence, _logger) = (db, presence, logger);

    public static string NormalizeNickname(string nickname) =>
        string.IsNullOrWhiteSpace(nickname) ? nickname : (nickname.StartsWith('#') ? nickname : "#" + nickname);

    public static string DisplayName(string nickname) =>
        string.IsNullOrEmpty(nickname) ? nickname : (nickname.StartsWith('#') ? nickname[1..] : nickname);

    public Task<User?> FindByConnectionIdAsync(string connectionId) =>
        _db.Users.FirstOrDefaultAsync(u => u.ConnectionId == connectionId);

    public Task<User?> FindByNicknameAsync(string nickname) =>
        _db.Users.FirstOrDefaultAsync(u => u.Nickname == NormalizeNickname(nickname));

    public async Task<(User user, bool isNew)> RegisterOrLoginAsync(string nickname, string password)
    {
        var nickCheck = InputValidator.ValidateNickname(nickname);
        if (!nickCheck.ok)
            throw new ArgumentException(nickCheck.error);

        var passCheck = InputValidator.ValidatePassword(password);
        if (!passCheck.ok)
            throw new ArgumentException(passCheck.error);

        var normalizedNick = NormalizeNickname(nickname.Trim());
        var user = await FindByNicknameAsync(normalizedNick);

        if (user == null)
        {
            user = new User
            {
                Nickname = normalizedNick,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
                IsApproved = true,
                SessionToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Зарегистрирован новый пользователь: {Nickname}", normalizedNick);
            return (user, true);
        }

        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Неверный пароль для пользователя: {Nickname}", normalizedNick);
            throw new UnauthorizedAccessException("Неверный ник или пароль");
        }

        user.SessionToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await _db.SaveChangesAsync();
        _logger.LogInformation("Успешный вход пользователя: {Nickname}", normalizedNick);
        return (user, false);
    }

    public async Task<User> JoinByTokenAsync(string nickname, string sessionToken)
    {
        var normalizedNick = NormalizeNickname(nickname);
        var user = await FindByNicknameAsync(normalizedNick);

        if (user == null || user.SessionToken != sessionToken)
        {
            _logger.LogWarning("Недействительный токен: {Nickname}", normalizedNick);
            throw new UnauthorizedAccessException("Сессия истекла или недействительна");
        }

        return user;
    }

    public async Task SetUserOnlineAsync(string connectionId, string nickname)
    {
        var user = await FindByNicknameAsync(nickname);
        if (user == null) return;

        user.ConnectionId = connectionId;
        user.IsOnline = true;
        user.LastSeen = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _presence.Connect(connectionId, nickname);
    }

    public async Task SetUserOfflineAsync(string connectionId)
    {
        var nick = _presence.NicknameOf(connectionId);
        var user = await FindByConnectionIdAsync(connectionId)
            ?? (nick != null ? await FindByNicknameAsync(nick) : null);

        _presence.Disconnect(connectionId);

        if (user == null) return;
        if (_presence.IsOnline(user.Nickname)) return;

        user.IsOnline = false;
        user.ConnectionId = null;
        user.LastSeen = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task InvalidateSessionAsync(string connectionId)
    {
        var nick = _presence.NicknameOf(connectionId);
        var user = await FindByConnectionIdAsync(connectionId)
            ?? (nick != null ? await FindByNicknameAsync(nick) : null);

        if (user != null)
        {
            user.SessionToken = null;
            await _db.SaveChangesAsync();
        }

        await SetUserOfflineAsync(connectionId);
    }

    public async Task ResetStalePresenceAsync()
    {
        await _db.Users.ExecuteUpdateAsync(s => s
            .SetProperty(u => u.IsOnline, false)
            .SetProperty(u => u.ConnectionId, (string?)null));
    }

    public async Task<List<string>> GetOnlineUsersAsync() =>
        await _db.Users.AsNoTracking()
            .Where(u => u.IsOnline)
            .Select(u => u.Nickname)
            .ToListAsync();

    public async Task<List<UserListItemDto>> GetUsersAsync()
    {
        var users = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Nickname)
            .ToListAsync();

        return users
            .Select(u =>
            {
                var online = _presence.IsOnline(u.Nickname);
                return new UserListItemDto
                {
                    Nickname = u.Nickname,
                    DisplayName = DisplayName(u.Nickname),
                    IsOnline = online,
                    Status = online ? "онлайн" : FormatLastSeen(u.LastSeen),
                    LastSeen = u.LastSeen
                };
            })
            .OrderByDescending(u => u.IsOnline)
            .ThenBy(u => u.DisplayName)
            .ToList();
    }

    private static string FormatLastSeen(DateTime lastSeenUtc)
    {
        var age = DateTime.UtcNow - lastSeenUtc;
        if (age.TotalMinutes < 15) return "Был(а) недавно";
        if (age.TotalHours < 24) return $"Был(а) {((int)age.TotalHours)} ч. назад";
        if (age.TotalDays < 7) return $"Был(а) {(int)age.TotalDays} дн. назад";
        return "Был(а) давно";
    }
}
