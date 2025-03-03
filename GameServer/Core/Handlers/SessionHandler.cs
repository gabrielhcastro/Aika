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
    private readonly SocketAsyncEventArgsPool _socketEventPool;

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

    public SocketAsyncEventArgs RentSocketEvent(EventHandler<SocketAsyncEventArgs> completedHandler) {
        var args = _socketEventPool.Rent();
        if(args == null) {
            args = new SocketAsyncEventArgs();
            args.Completed += completedHandler;
        }
        else {
            args.Completed -= completedHandler;
            args.Completed += completedHandler;
            args.AcceptSocket = null;
            args.UserToken = null;
            args.SetBuffer(null, 0, 0);
        }
        return args;
    }

    public void ReturnSocketEvent(SocketAsyncEventArgs eventArg) {
        _socketEventPool.Return(eventArg);
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

    private void ReadComplete(object sender, SocketAsyncEventArgs e) {
        if(e.LastOperation == SocketAsyncOperation.Receive) {
            ProcessReceive(e);
        }
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

    public int GetAllSessionsCount() => _sessions.Values.Count;
    public List<Session> GetAllSessions() => [.. _sessions.Values];
    public static int GetAllCharacters() => _characters.Values.Count;
}