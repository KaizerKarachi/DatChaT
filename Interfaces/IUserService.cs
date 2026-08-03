using FamilyChat.Models;
namespace FamilyChat.Interfaces;
public interface IUserService
{
    Task<User?> FindByConnectionIdAsync(string connectionId);
    Task<User?> FindByNicknameAsync(string nickname);
    Task<User> RegisterOrLoginAsync(string nickname, string password);
    Task<User> JoinByTokenAsync(string nickname, string sessionToken);
    Task SetUserOnlineAsync(string connectionId, string nickname);
    Task SetUserOfflineAsync(string connectionId);
    Task<List<string>> GetOnlineUsersAsync();
}
