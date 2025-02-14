using GameServer.Core.Base;
using GameServer.Core.Instance;
using GameServer.Models;
using System.Collections.Concurrent;

namespace GameServer.Handlers;
public class SessionHandler : Singleton<SessionHandler> {
    private readonly ConcurrentDictionary<uint, Session> _sessions = new();
    private static readonly Dictionary<int, AccountEntitie> _onlinePlayers = new();

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

    public void UpdateSessionActivity(Session session) {
        Console.WriteLine("Session Activity Updated: {0}", session.Id);
        session.LastActivity = DateTime.UtcNow;
    }

    public void AddPlayer(int playerId, AccountEntitie player) {
        _onlinePlayers[playerId] = player;
    }

    public static AccountEntitie GetPlayer(int playerId) {
        _onlinePlayers.TryGetValue(playerId, out var player);
        return player;
    }

    public static void RemovePlayer(int playerId) {
        _onlinePlayers.Remove(playerId);
    }

    public int GetAllSessionsValue() => _sessions.Values.Count;
    public List<Session> GetAllSessions() => [.. _sessions.Values];
    public static int GetAllPlayers() => _onlinePlayers.Values.Count;
}
