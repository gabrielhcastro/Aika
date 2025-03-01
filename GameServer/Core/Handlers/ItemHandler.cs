using GameServer.Core.Base;
using GameServer.Model.Item;
using GameServer.Network;
using GameServer.Service;

namespace GameServer.Core.Handlers;

public static class ItemHandler {
    public static void UpdateItem(Session session, ItemEntitie item, bool notice) {
        var packet = PacketFactory.CreateHeader(0xF0E, 0x7535);

        packet.Write(notice);
        packet.Write(item.SlotType);
        packet.Write((ushort)item.Slot);

        ItemService.WriteSingleItemOnPacket(packet, item);

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    public static void UpdateEquips(Session session, bool notice) {
        var orderedEquips = CharacterService.GetCharEquipsInitOrdered(session.ActiveCharacter.Equips);

        foreach(var equip in orderedEquips) {
            UpdateItem(session, equip.Value, notice);
        }
    }

    public static void UpdateInventory(Session session, bool notice) {
        var orderedItens = CharacterService.GetCharInventoryOrdered(session.ActiveCharacter.Inventory);

        foreach(var item in orderedItens) {
            UpdateItem(session, item.Value, notice);
        }
    }
}
