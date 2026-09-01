using System.Collections.Concurrent;

namespace FamilyChat.Services;

public sealed class PresenceTracker
{
    private readonly ConcurrentDictionary<string, string> _nickByConnection = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, HashSet<string>> _connectionsByNick = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public static string FamilyGroup => "family";
    public static string UserGroup(string nickname) => "user:" + UserService.NormalizeNickname(nickname);

    public void Connect(string connectionId, string nickname)
    {
        var nick = UserService.NormalizeNickname(nickname);
        lock (_gate)
        {
            if (_nickByConnection.TryGetValue(connectionId, out var old) && old != nick)
                RemoveLocked(connectionId, old);

            _nickByConnection[connectionId] = nick;
            var set = _connectionsByNick.GetOrAdd(nick, _ => []);
            set.Add(connectionId);
        }
    }

    public string? Disconnect(string connectionId)
    {
        lock (_gate)
        {
            if (!_nickByConnection.TryRemove(connectionId, out var nick))
                return null;
            RemoveLocked(connectionId, nick);
            return nick;
        }
    }

    public bool IsOnline(string nickname)
    {
        var nick = UserService.NormalizeNickname(nickname);
        return _connectionsByNick.TryGetValue(nick, out var set) && set.Count > 0;
    }

    public string? NicknameOf(string connectionId) =>
        _nickByConnection.TryGetValue(connectionId, out var nick) ? nick : null;

    private void RemoveLocked(string connectionId, string nick)
    {
        if (!_connectionsByNick.TryGetValue(nick, out var set)) return;
        set.Remove(connectionId);
        if (set.Count == 0)
            _connectionsByNick.TryRemove(nick, out _);
    }
}
