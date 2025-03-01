using GameServer.Model.Item;
using MySqlConnector;

namespace GameServer.Data.Repositories;
public class ItemRepository {
    public static async Task<List<ItemEntitie>> GetCharEquipsAsync(uint characterId) {
        List<ItemEntitie> equips = [];

        await using var connection = await DatabaseHandler.GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT * FROM itens WHERE ownerId = @ownerId AND slotType = 0", connection);
        command.Parameters.AddWithValue("@ownerId", characterId);

        await using var reader = await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            ItemEntitie equip = new() {
                OwnerId = reader.GetUInt32("ownerId"),
                Slot = reader.GetByte("slot"),
                SlotType = reader.GetByte("slotType"),
                ItemId = reader.GetUInt32("itemId"),
                App = reader.GetUInt32("app"),
                MinimalValue = reader.GetUInt32("minimalItemValue"),
                MaxValue = reader.GetUInt32("maxItemValue"),
                Refine = reader.GetUInt16("refine"),
                Time = reader.GetUInt32("time")
            };

            equips.Add(equip);
        }

        Console.WriteLine($"Personagem ID [{characterId}] - Equipamentos encontrados: {equips.Count}");
        return equips;
    }

    public static async Task<List<ItemEntitie>> GetCharInventoryAsync(uint characterId) {
        List<ItemEntitie> itens = [];

        await using var connection = await DatabaseHandler.GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT * FROM itens WHERE ownerId = @ownerId AND slotType = 1", connection);
        command.Parameters.AddWithValue("@ownerId", characterId);

        await using var reader = await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            ItemEntitie item = new() {
                OwnerId = reader.GetUInt32("ownerId"),
                Slot = reader.GetByte("slot"),
                SlotType = reader.GetByte("slotType"),
                ItemId = reader.GetUInt32("itemId"),
                App = reader.GetUInt32("app"),
                MinimalValue = reader.GetUInt32("minimalItemValue"),
                MaxValue = reader.GetUInt32("maxItemValue"),
                Refine = reader.GetUInt16("refine"),
                Time = reader.GetUInt32("time")
            };

            itens.Add(item);
        }

        Console.WriteLine($"Personagem ID [{characterId}] - Itens encontrados: {itens.Count}");
        return itens;
    }
}
