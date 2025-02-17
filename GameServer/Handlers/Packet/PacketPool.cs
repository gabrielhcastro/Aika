using System.Collections.Concurrent;

namespace GameServer.Handlers.Packet;

public static class PacketPool {
    private static readonly ConcurrentBag<StreamHandler> _packetPool = [];

    public static StreamHandler Rent() {
        if(_packetPool.TryTake(out var packet)) {
            packet.Reset(); // Limpa o buffer antes de reutilizar
            return packet;
        }
        return new StreamHandler(); // pool vazio
    }

    public static void Return(StreamHandler packet) {
        _packetPool.Add(packet);
    }
}
