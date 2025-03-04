using GameServer.Core.Handlers;
using System.Collections.Concurrent;

namespace GameServer.Network;

public static class PacketPool {
    private static readonly ConcurrentBag<StreamHandler> _packetPool = [];

    public static StreamHandler Rent() {
        if(_packetPool.TryTake(out var packet)) {
            packet.Reset();
            return packet;
        }
        return new StreamHandler();
    }

    public static void Return(StreamHandler packet) {
        _packetPool.Add(packet);
    }
}
