using GameServer.Core.Base;
using GameServer.Core.Instance;
using GameServer.GameLogic;
using System.Collections.Concurrent;

namespace GameServer.Handlers;
public class SessionHandler : Singleton<SessionHandler> {
    private readonly ConcurrentDictionary<uint, Session> _sessions = new();
    private static readonly Dictionary<int, Player> _onlinePlayers = new();

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

    public void AddPlayer(int playerId, Player player) {
        _onlinePlayers[playerId] = player;
    }

    public static Player GetPlayer(int playerId) {
        _onlinePlayers.TryGetValue(playerId, out var player);
        return player;
    }

    public static void RemovePlayer(int playerId) {
        _onlinePlayers.Remove(playerId);
    }

    public static int GetAllPlayers() => _onlinePlayers.Values.Count;
    public int GetAllSessionsValue() => _sessions.Values.Count;
    public List<Session> GetAllSessions() => _sessions.Values.ToList();
}
