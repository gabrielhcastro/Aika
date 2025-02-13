using GameServer.Handlers.Instance;
using GameServer.Network;
using System.Collections.Concurrent;

namespace GameServer.Handlers.Sessions;
public class SessionHandler : Singleton<SessionHandler> {
    private readonly ConcurrentDictionary<uint, Session> _sessions = new();
    private readonly Timer _sessionCheckTimer;
    private readonly int _timeoutSeconds = 10;

    public SessionHandler() {
        //_sessionCheckTimer = new Timer(CheckSessions, null, 10000, 10000);
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

    public List<Session> GetAllSessions() {
        return _sessions.Values.ToList();
    }

    private void CheckSessions(object state) {
        var now = DateTime.UtcNow;
        foreach(var session in _sessions.Values.ToList()) {
            if((now - session.LastPongTime).TotalSeconds > _timeoutSeconds) {
                Console.WriteLine($"Desconectando sessão inativa: {session.Ip}");
                RemoveSession(session);
            }
        }
    }
}
