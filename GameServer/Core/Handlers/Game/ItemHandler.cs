using GameServer.Core.Base;
using GameServer.Model.Item;
using GameServer.Network;
using GameServer.Service;

namespace GameServer.Core.Handlers.InGame;

public static class ItemHandler {
    public static void UpdateItemBySlotAndType(Session session, ItemEntitie item, bool notice) {
        var account = session.ActiveAccount;
        var connectionId = account.Id - 1 + 0x7535;
        var packet = PacketFactory.CreateHeader(0xF0E, (ushort)connectionId);

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

    public static void SetLobbyEquipsOrdered(Session session, bool notice) {
        var lobbyOrderedEquips = CharacterService.GetLobbyEquipsOrdered(session.ActiveCharacter.Equips);

        foreach(var equip in lobbyOrderedEquips) {
            UpdateItemBySlotAndType(session, equip.Value, notice);
        }
    }

    public static void SetEquipsOrdered(Session session, bool notice) {
        var lobbyOrderedEquips = CharacterService.GetEquipsOrdered(session.ActiveCharacter.Equips);

        foreach(var equip in lobbyOrderedEquips) {
            UpdateItemBySlotAndType(session, equip.Value, notice);
        }
    }

    public static void UpdateInventoryItensOrdered(Session session, bool notice) {
        var orderedInventory = CharacterService.GetInventoryOrdered(session.ActiveCharacter.Inventory);

        foreach(var item in orderedInventory) {
            UpdateItemBySlotAndType(session, item.Value, notice);
        }
    }
}
