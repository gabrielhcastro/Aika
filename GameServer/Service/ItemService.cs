using GameServer.Core.Handlers.Core;
using GameServer.Data.Repositories;
using GameServer.Model.Character;
using GameServer.Model.Item;

namespace GameServer.Service;

public static class ItemService {
    public static async Task<List<ItemEntitie>> GetCharEquips(CharacterEntitie character) {
        return await ItemRepository.GetCharEquipsAsync(character.Id);
    }

    public static async Task<List<ItemEntitie>> GetCharInventory(CharacterEntitie character) {
        return await ItemRepository.GetCharInventoryAsync(character.Id);
    }

    public static void SetLobbyEquips(CharacterEntitie character, StreamHandler stream) {
        var orderedEquips = GetLobbyEquipsOrdered(character?.Equips);

        for(int i = 0; i < 8; i++) {
            stream.Write((ushort)orderedEquips[i].ItemId);
        }
    }

    public static void SetEquipsOrdered(CharacterEntitie character, StreamHandler stream) {
        var orderedEquips = GetEquipsOrdered(character?.Equips);

        for(int i = 0; i < 16; i++) stream.Write((ushort)orderedEquips[i].ItemId);
    }

    public static void SetInventoryOrdered(CharacterEntitie character, StreamHandler stream) {
        var orderedInventory = GetInventoryOrdered(character?.Inventory);

        for(int i = 0; i < 60; i++) stream.Write((ushort)orderedInventory[i].ItemId);
    }

    private static Dictionary<int, ItemEntitie> OrderItems(List<ItemEntitie> items, int maxSlots) {
        var orderedItems = Enumerable.Range(0, maxSlots).ToDictionary(i => i, _ => new ItemEntitie());

        foreach(var item in items ?? []) {
            if(item.Slot >= 0 && item.Slot < maxSlots) orderedItems[item.Slot] = item;
        }

        return orderedItems;
    }

    public static Dictionary<int, ItemEntitie> GetInventoryOrdered(List<ItemEntitie> inventory)
    => OrderItems(inventory, 60);

    public static Dictionary<int, ItemEntitie> GetEquipsOrdered(List<ItemEntitie> equips)
        => OrderItems(equips, 16);

    public static Dictionary<int, ItemEntitie> GetLobbyEquipsOrdered(List<ItemEntitie> equips)
        => OrderItems(equips, 8);
}
