using GameServer.Core.Base;
using GameServer.Model.Character;
using GameServer.Network;
using Shared.Core;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace GameServer.Core.Handlers;
public class SessionHandler : Singleton<SessionHandler> {
    private readonly ConcurrentDictionary<uint, Session> _sessions = [];
    private static readonly Dictionary<int, CharacterEntitie> _characters = [];
    private readonly ObjectPool<Session> _sessionPool = new();

    public SessionHandler() {
    }

    public Session RentSession(SocketAsyncEventArgs readEventArg, Socket socket) {
        var session = _sessionPool.Rent() ?? new Session();
        return session;
    }

    public void ReturnSession(Session session) {
        _sessionPool.Return(session);
    }

    public void AddSession(Session session) {
        _sessions[session.Id] = session;
        session.LastActivity = DateTime.UtcNow;
        UpdateSessionActivity(session);
    }
    public void RemoveSession(Session session) {
        _sessions.TryRemove(session.Id, out _);
        session.ActiveCharacter = null;
        session.ActiveAccount = null;

        UpdateSessionActivity(session);
        session.Close();
    }

    public static void UpdateSessionActivity(Session session) {
        session.LastActivity = DateTime.UtcNow;
    }

    public static void AddCharacter(CharacterEntitie character) {
        _characters.TryAdd(GetAllCharacters() + 1, character);
        Console.WriteLine($"Player logou: {character.Name}.");
    }

    public static CharacterEntitie GetCharacter(int playerId) {
        _characters.TryGetValue(playerId, out var player);
        return player;
    }

    public static void RemoveCharacter(int playerId) {
        _characters.Remove(playerId, out var character);
        Console.WriteLine($"Player deslogou: {character?.Name}.");
    }

    public int GetAllSessionsCount() => _sessions.Values.Count;
    public List<Session> GetAllSessions() => [.. _sessions.Values];
    public static int GetAllCharacters() => _characters.Values.Count;
}