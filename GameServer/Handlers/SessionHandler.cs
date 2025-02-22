using GameServer.Core.Base;
using Shared.Core.Instance;
using Shared.Models.Account;
using Shared.Models.Character;
using System.Collections.Concurrent;
using System.Reflection.PortableExecutable;

namespace GameServer.Handlers;
public class SessionHandler : Singleton<SessionHandler> {
    private readonly ConcurrentDictionary<uint, Session> _sessions = [];
    private static readonly Dictionary<int, CharacterEntitie> _characters = [];

    public SessionHandler() {
    }

    /// <summary>
    /// Adiciona sessão ao servidor e atualiza o tempo da ultima atividade
    /// </summary>
    public void AddSession(Session session) {
        Console.WriteLine("Session Add: {0}", session.Id);
        _sessions[session.Id] = session;
        session.LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Limpa os personagens da sessão e a remove do servidor
    /// </summary>
    public void RemoveSession(Session session) {
        Console.WriteLine("Session Removed: {0}", session.Id);
        _sessions.TryRemove(session.Id, out _);
        session.ActiveCharacter = null;

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
