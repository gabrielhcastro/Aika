using Shared.Network;

namespace Shared.Base;

public interface INetwork {
    void OnConnect(Session session);
    void OnReceive(Session session, byte[] buf, int bytes);
    void OnSend(Session session, byte[] buf, int offset, int bytes);
    void OnDisconnect(Session session);
    void RemoveSession(Session session);
}