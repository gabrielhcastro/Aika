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

    public Session GetSessionByCharacterId(ushort characterId) {
        return _sessions.Values.FirstOrDefault(s => s.ActiveCharacter?.Id == characterId);
    }

    public int GetAllSessionsCount() => _sessions.Values.Count;

    public List<Session> GetAllSessions() => [.. _sessions.Values];

    public static int GetAllCharacters() => _characters.Values.Count;

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

    // TO-DO: REMOÇÃO DE PERSONAGEM AO DESLOGAR
    public static void RemoveCharacter(int playerId) {
        _characters.Remove(playerId, out var character);
        Console.WriteLine($"Player deslogou: {character?.Name}.");
    }

    public void UpdateVisibleList(Session session) {
        //float visibilityRange = 30f;
        if(session.ActiveCharacter == null) return;

        var character = session.ActiveCharacter;
        character.Neighbors?.Clear();

        foreach(var otherSession in _sessions.Values) {
            if(session == otherSession || otherSession.ActiveCharacter == null)
                continue;

            var otherCharacter = otherSession.ActiveCharacter;

            if(!character.VisiblePlayers.Contains((ushort)otherCharacter.Id)) {
                CharacterHandler.CreateCharacterMob(otherSession, 0);

                character.VisiblePlayers.Add((ushort)otherCharacter.Id);
            }
            //float distance = session.ActiveCharacter.Position.Distance(otherSession.ActiveCharacter.Position);

            //    if(distance <= visibilityRange) {
            //        if(!session.ActiveCharacter.VisiblePlayers.Contains((ushort)otherSession.Id)) {
            //            session.ActiveCharacter.VisiblePlayers.Add((ushort)otherSession.Id);
            //            var foundSession = GetSession((ushort)otherSession.Id);
            //            if(foundSession != null) {
            //                CharacterHandler.CreateCharacterMob(foundSession);
            //            }

            //            CharacterHandler.GameMessage(session, 16, 0, "Tentando adicionar personagem");
            //        }
            //    }
            //    else {
            //        if(session.ActiveCharacter.VisiblePlayers.Contains((ushort)otherSession.Id)) {
            //            session.ActiveCharacter.VisiblePlayers.Remove((ushort)otherSession.Id);
            //            CharacterHandler.GameMessage(session, 16, 0, "Tentando remover personagem");
            //        }
            //    }
            //}

            //foreach(var npc in NpcHandler.Instance.GetAllNpcs()) {
            //    float distance = session.ActiveCharacter.DistanceTo(npc);

            //    if(distance <= visibilityRange) {
            //        if(!session.VisibleNpcs.Contains(npc.Id)) {
            //            session.VisibleNpcs.Add(npc.Id);
            //            session.SendPacket(PacketFactory.CreateSpawnNpc(npc));
            //        }
            //    }
            //    else {
            //        if(session.VisibleNpcs.Contains(npc.Id)) {
            //            session.VisibleNpcs.Remove(npc.Id);
            //            session.SendPacket(PacketFactory.CreateRemoveNpc(npc));
            //        }
            //    }
            //}
        }
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
}