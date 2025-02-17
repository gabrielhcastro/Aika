using GameServer.Core.Base;
using Shared.Core.Instance;
using Shared.Models.Account;
using System.Collections.Concurrent;

namespace GameServer.Handlers;
public class SessionHandler : Singleton<SessionHandler> {
    private readonly ConcurrentDictionary<uint, Session> _sessions = [];
    private static readonly Dictionary<int, AccountEntitie> _onlinePlayers = [];

    public SessionHandler() {
    }

    public void AddSession(Session session) {
        Console.WriteLine("Session Add: {0}", session.Id);
        _sessions[session.Id] = session;
        session.LastActivity = DateTime.UtcNow;
    }

    public void RemoveSession(Session session) {
        Console.WriteLine("Session Removed: {0}", session.Id);
        _sessions.TryRemove(session.Id, out _);
        session.Close();
    }

    public static void UpdateSessionActivity(Session session) {
        Console.WriteLine("Session Activity Updated: {0}", session.Id);
        session.LastActivity = DateTime.UtcNow;
    }

    public static void AddPlayer(int playerId, AccountEntitie player) {
        _onlinePlayers[playerId] = player;
    }

    public static AccountEntitie GetPlayer(int playerId) {
        _onlinePlayers.TryGetValue(playerId, out var player);
        return player;
    }

    public static void RemovePlayer(int playerId) {
        _onlinePlayers.Remove(playerId);
    }

    public int GetAllSessionsCount() => _sessions.Values.Count;
    public List<Session> GetAllSessions() => [.. _sessions.Values];
    public static int GetAllPlayers() => _onlinePlayers.Values.Count;
}
