
using GameServer.Core.Handlers;
using GameServer.Model.Item;

namespace GameServer.Service;

public static class ItemService {
    public static void WriteSingleItemOnPacket(StreamHandler packet, ItemEntitie item) {
        packet.Write(item.ItemId);
        packet.Write(item.App);
        packet.Write(item.Identification);

        // Index Buff Effect
        for(int i = 0; i < 3; i++) {
            packet.Write((byte)0);
        }

        // Value Buff Effect
        for(int i = 0; i < 3; i++) {
            packet.Write((byte)0);
        }

        packet.Write((byte)item.MinimalValue);
        packet.Write((byte)item.MaxValue);

        packet.Write(item.Refine);
        packet.Write((ushort)item.Time);
    }
}
