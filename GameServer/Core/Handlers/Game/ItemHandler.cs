using GameServer.Core.Base;
using GameServer.Core.Handlers.Core;
using GameServer.Model.Item;
using GameServer.Network;

namespace GameServer.Core.Handlers.InGame;

public static class ItemHandler {
    public static void UpdateItemBySlotAndSlotType(Session session, ItemEntitie item, bool notice) {
        var account = session.ActiveAccount;
        var connectionId = account.ConnectionId;

        var packet = CreateUpdateItemBySlotAndSlotTypePacket(connectionId, item, notice);

        session.SendPacket(packet);
    }

    public static byte[] CreateUpdateItemBySlotAndSlotTypePacket(ushort connectionId, ItemEntitie item, bool notice) {
        var packet = PacketFactory.CreateHeader(0xF0E, connectionId);
        packet.Write(notice);
        packet.Write(item.SlotType);
        packet.Write((ushort)item.Slot);
        WriteItemOnPacket(packet, item);

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        return packetData;
    }

    public static void WriteItemOnPacket(StreamHandler packet, ItemEntitie item) {
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
