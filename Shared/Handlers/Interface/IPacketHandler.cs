using Shared.Network;

namespace Shared.Handlers.Interface;
public interface IPacketHandler {
    void Process(Session session);
}
