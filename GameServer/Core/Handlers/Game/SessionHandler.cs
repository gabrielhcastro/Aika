using GameServer.Core.Base;
using GameServer.Core.Handlers.Game;
using GameServer.Model.Character;
using GameServer.Network;
using Shared.Core;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace GameServer.Core.Handlers.InGame;
public class SessionHandler : Singleton<SessionHandler> {
    private readonly ConcurrentDictionary<uint, Session> _sessions = [];
    private readonly ObjectPool<Session> _sessionPool = new();
    private readonly SocketAsyncEventArgsPool _socketEventPool;
    public static readonly Dictionary<ushort, CharacterEntitie> _characters = [];

    public SessionHandler() {
        _socketEventPool = new SocketAsyncEventArgsPool(100, ReadComplete);
    }

    public Session RentSession(INetwork network, SocketAsyncEventArgs readEventArg, Socket socket) {
        var session = _sessionPool.Rent() ?? new Session();
        session.Init(network, readEventArg, socket);
        return session;
    }

    public void ReturnSession(Session session) {
        session.Reset();
        _sessionPool.Return(session);
    }

    public SocketAsyncEventArgs RentSocketEvent() {
        var args = SocketAsyncEventArgsPool.Instance.Rent();
        args.Completed -= ReadComplete;
        args.Completed += ReadComplete;
        return args;
    }

    public void ReturnSocketEvent(SocketAsyncEventArgs eventArg) => _socketEventPool.Return(eventArg);

    public void AddSession(Session session) {
        _sessions[session.Id] = session;
        UpdateSessionActivity(session);
    }

    public void RemoveSession(Session session) {
        if(_sessions.TryRemove(session.Id, out _)) {
            if(session.ActiveCharacter != null) {
                GridHandler.Instance.RemoveCharacter(session.ActiveCharacter);
                RemoveChar((ushort)session.ActiveCharacter.Id);
            }

            session.ActiveCharacter = null;
            session.ActiveAccount = null;
            session.Close();
        }

        UpdateSessionActivity(session);
    }

    public int GetAllSessionsCount() => _sessions.Values.Count;

    public List<Session> GetAllSessions() => [.. _sessions.Values];

    public static ushort GetAllCharCount() => (ushort)_characters.Values.Count;

    public static void UpdateSessionActivity(Session session) => session.LastActivity = DateTime.UtcNow;

    public Session GetSessionByCharId(ushort characterId) =>
        _sessions.Values.FirstOrDefault(s => s.ActiveCharacter?.Id == characterId);

    public static CharacterEntitie GetChar(ushort playerId) =>
        _characters.TryGetValue(playerId, out var player) ? player : null;

    public static void AddChar(CharacterEntitie character) {
        _characters.TryAdd((ushort)(GetAllCharCount() + 1), character);
        Console.WriteLine($"Player logou: {character.Name}.");
    }

    public static void RemoveChar(ushort playerId) {
        if(_characters.Remove(playerId, out var character)) {
            Console.WriteLine($"Player deslogou: {character?.Name}.");
            GridHandler.Instance.RemoveCharacter(character);
        }
    }

    public void UpdateVisibleList(Session session) {
        if(session.ActiveCharacter == null) return;

        var character = session.ActiveCharacter;
        character.VisiblePlayers ??= [];

        foreach(var otherSession in _sessions.Values.Where(oS => oS.Id != session.Id && oS.ActiveCharacter != null)) {
            var otherCharacter = otherSession.ActiveCharacter!;
            if(character.VisiblePlayers.Contains((ushort)otherCharacter.Id)) continue;
            var distance = character.Position.Distance(otherCharacter.Position);
                    
            var nearbyCharacters = GridHandler.Instance.GetNearbyCharacters(character.Position, 25);

            foreach(var nearbyCharacter in nearbyCharacters) {
                if(nearbyCharacter.Id == character.Id) continue;

                var nearbySession = GetSessionByCharId((ushort)nearbyCharacter.Id);

                if(!character.VisiblePlayers.Contains((ushort)nearbyCharacter.Id)) {
                    character.VisiblePlayers.Add((ushort)nearbyCharacter.Id);
                    CharacterHandler.CreateMob(session, nearbyCharacter, nearbySession.ActiveAccount.ConnectionId, 1);
                }

                if(!nearbyCharacter.VisiblePlayers.Contains((ushort)character.Id)) {
                    nearbyCharacter.VisiblePlayers.Add((ushort)character.Id);
                    CharacterHandler.CreateMob(nearbySession, character, session.ActiveAccount.ConnectionId, 1);
                }
            }
        }
    }

    private void ReadComplete(object sender, SocketAsyncEventArgs e) {
        if(e.LastOperation == SocketAsyncOperation.Receive)             ProcessReceive(e);
        else {
            throw new ArgumentException("The last operation completed on the socket was not a receive.");
        }
    }

    private void ProcessReceive(SocketAsyncEventArgs e) {
        var session = (Session)e.UserToken;
        if(e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {
            Span<byte> buffer = e.BytesTransferred <= 128
                ? stackalloc byte[e.BytesTransferred]
                : new byte[e.BytesTransferred];

            e.Buffer.AsSpan(e.Offset, e.BytesTransferred).CopyTo(buffer);
            session.Network.OnReceive(session, buffer.ToArray(), e.BytesTransferred);

            try {
                var willRaiseEvent = session.Socket.ReceiveAsync(e);
                if(!willRaiseEvent)
                    ProcessReceive(e);
            }
            catch(ObjectDisposedException) {
                session.Close();
            }
        }
        else {
            session.Close();
        }
    }
}