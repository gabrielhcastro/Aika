using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Model.Item;
using GameServer.Service;
using MySqlConnector;
using System.Data;

namespace GameServer.Data.Repositories;
public static class CharacterRepository {
    public static readonly Dictionary<ushort,
        (int Strength, int Intelligence, int Agility, int Constitution, int Luck, List<ItemEntitie> Items)>
        InitialClassItensAndStatus = new() {
            // WR
            [0] = (15, 5, 9, 16, 0, new List<ItemEntitie> {
        new() { Slot = 3, SlotType = 0, ItemId = 1719, App = 1719, MinimalValue = 100, MaxValue = 100 },
        new() { Slot = 5, SlotType = 0, ItemId = 1779, App = 1779, MinimalValue = 100, MaxValue = 100 },
        new() { Slot = 6, SlotType = 0, ItemId = 1069, App = 1069, MinimalValue = 160, MaxValue = 160 }
        }),

            // TP
            [1] = (14, 6, 10, 14, 0, new List<ItemEntitie> {
        new() { Slot = 3, SlotType = 0, ItemId = 1839, App = 1839, MinimalValue = 120, MaxValue = 120 },
        new() { Slot = 5, SlotType = 0, ItemId = 1899, App = 1899, MinimalValue = 120, MaxValue = 120 },
        new() { Slot = 6, SlotType = 0, ItemId = 1034, App = 1034, MinimalValue = 140, MaxValue = 140 },
        new() { Slot = 7, SlotType = 0, ItemId = 1309, App = 1309, MinimalValue = 120, MaxValue = 120 }
        }),

            // ATT
            [2] = (8, 9, 16, 12, 5, new List<ItemEntitie> {
        new() { Slot = 3, SlotType = 0, ItemId = 1959, App = 1959, MinimalValue = 80, MaxValue = 80 },
        new() { Slot = 5, SlotType = 0, ItemId = 2019, App = 2019, MinimalValue = 80, MaxValue = 80 },
        new() { Slot = 6, SlotType = 0, ItemId = 1209, App = 1209, MinimalValue = 160, MaxValue = 160 }
        }),

            // DUAL
            [3] = (8, 10, 14, 12, 6, new List<ItemEntitie> {
        new() { Slot = 3, SlotType = 0, ItemId = 2079, App = 2079, MinimalValue = 80, MaxValue = 80 },
        new() { Slot = 5, SlotType = 0, ItemId = 2139, App = 2139, MinimalValue = 80, MaxValue = 80 },
        new() { Slot = 6, SlotType = 0, ItemId = 1174, App = 1174, MinimalValue = 140, MaxValue = 140 }
        }),

            // FC
            [4] = (7, 16, 9, 8, 10, new List<ItemEntitie> {
        new() { Slot = 3, SlotType = 0, ItemId = 2199, App = 2199, MinimalValue = 60, MaxValue = 60 },
        new() { Slot = 5, SlotType = 0, ItemId = 2259, App = 2259, MinimalValue = 60, MaxValue = 60 },
        new() { Slot = 6, SlotType = 0, ItemId = 1279, App = 1279, MinimalValue = 160, MaxValue = 160 }
        }),

            // CL
            [5] = (7, 15, 10, 9, 9, new List<ItemEntitie> {
        new() { Slot = 3, SlotType = 0, ItemId = 2319, App = 2319, MinimalValue = 60, MaxValue = 60 },
        new() { Slot = 5, SlotType = 0, ItemId = 2379, App = 2379, MinimalValue = 60, MaxValue = 60 },
        new() { Slot = 6, SlotType = 0, ItemId = 1244, App = 1244, MinimalValue = 140, MaxValue = 140 }
        })
        };
    
    public static async Task<List<CharacterEntitie>> GetCharactersByAccountIdAsync(uint accountId) {
        List<CharacterEntitie> characters = [];

        await using var connection = await DatabaseHandler.GetConnectionAsync();

        await using var command = new MySqlCommand("SELECT * FROM characters WHERE ownerAccountId = @accountId", connection);
        command.Parameters.AddWithValue("@accountId", accountId);

        await using var reader = await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            var character = new CharacterEntitie {
                Id = reader.GetUInt32("id"),
                OwnerAccountId = reader.GetUInt32("ownerAccountId"),
                Name = reader.GetString("name"),
                Slot = reader.GetByte("slot"),
                NumericToken = reader.IsDBNull("numericToken") ? string.Empty : reader.GetString("numericToken"),
                NumericErrors = reader.IsDBNull("numericErrors") ? (byte)0 : reader.GetByte("numericErrors"),
                Deleted = reader.GetByte("deleted"),
                SpeedMove = reader.IsDBNull("speedMove") ? (byte)0 : reader.GetByte("speedMove"),
                Rotation = reader.IsDBNull("rotation") ? (byte)0 : reader.GetByte("rotation"),
                LastLogin = reader.IsDBNull("lastLogin") ? "N/A" : reader.GetString("lastLogin"),
                PlayerKill = reader.GetByte("playerKill"),
                ClassInfo = reader.GetByte("classInfo"),
                FirstLogin = reader.IsDBNull("firstLogin") ? (byte)0 : reader.GetByte("firstLogin"),
                Strength = reader.GetUInt32("strength"),
                Agility = reader.GetUInt32("agility"),
                Intelligence = reader.GetUInt32("intelligence"),
                Constitution = reader.GetUInt32("constitution"),
                Luck = reader.GetUInt32("luck"),
                Status = reader.GetUInt32("status"),
                Height = reader.GetByte("height"),
                Trunk = reader.GetByte("trunk"),
                Leg = reader.GetByte("leg"),
                Body = reader.GetByte("body"),
                CurrentHealth = reader.IsDBNull("currentHealth") ? 0 : reader.GetUInt32("currentHealth"),
                CurrentMana = reader.IsDBNull("currentMana") ? 0 : reader.GetUInt32("currentMana"),
                Honor = reader.IsDBNull("honor") ? 0 : reader.GetUInt32("honor"),
                KillPoint = reader.IsDBNull("killPoint") ? 0 : reader.GetUInt32("killPoint"),
                Infamia = reader.IsDBNull("infamia") ? (byte)0 : reader.GetByte("infamia"),
                SkillPoint = reader.IsDBNull("skillPoint") ? 0 : reader.GetUInt32("skillPoint"),
                Experience = reader.GetUInt64("experience"),
                Level = reader.GetByte("level"),
                GuildIndex = reader.IsDBNull("guildIndex") ? 0 : reader.GetUInt32("guildIndex"),
                Gold = reader.IsDBNull("gold") ? 0 : reader.GetUInt32("gold"),
                PositionX = reader.GetUInt32("positionX"),
                PositionY = reader.GetUInt32("positionY"),
                CreationTime = reader.GetString("creationTime"),
                DeleteTime = reader.IsDBNull("deleteTime") ? string.Empty : reader.GetString("deleteTime"),
                LoginTime = reader.IsDBNull("loginTime") ? 0 : reader.GetUInt32("loginTime"),
                ActiveTitle = reader.IsDBNull("activeTitle") ? 0 : reader.GetUInt32("activeTitle"),
                ActiveAction = reader.IsDBNull("activeAction") ? 0 : reader.GetUInt32("activeAction"),
                TeleportPositions = reader.IsDBNull("teleportPositions") ? string.Empty : reader.GetString("teleportPositions"),
                PranEvolutionCount = reader.IsDBNull("pranEvolutionCount") ? 0 : reader.GetUInt32("pranEvolutionCount"),
                SavedPositionX = reader.IsDBNull("savedPositionX") ? 0 : reader.GetUInt32("savedPositionX"),
                SavedPositionY = reader.IsDBNull("savedPositionY") ? 0 : reader.GetUInt32("savedPositionY"),
            };

            character.Position = new(character.PositionX, character.Position.Y);
            character.Equips = await ItemService.GetCharEquips(character);
            character.Inventory = await ItemService.GetCharInventory(character);
            characters.Add(character);
        }

        return characters;
    }

    public static async Task<bool> CreateCharacterAsync(CharacterEntitie character, AccountEntitie account) {
        await using var connection = await DatabaseHandler.GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try {
            await using var command = new MySqlCommand(
                "INSERT INTO characters (ownerAccountId, name, slot, classInfo, positionX, positionY, height, trunk, leg, body, level, experience," +
                "strength, agility, constitution, intelligence, luck, status, creationTime, numericErrors, speedMove, firstLogin, currentHealth, currentMana) " +
                "VALUES (@ownerAccountId, @name, @slot, @classInfo, @positionX, @positionY, @height, @trunk, @leg, @body, @level, @experience," +
                "@strength, @agility, @constitution, @intelligence, @luck, @status, @creationTime, @numericErrors, @speedMove, @firstLogin, @currentHealth, @currentMana); " +
                "SELECT LAST_INSERT_ID();",
                connection, transaction
            );

            command.Parameters.AddWithValue("@ownerAccountId", account.Id);
            command.Parameters.AddWithValue("@name", character.Name);
            command.Parameters.AddWithValue("@slot", character.Slot);
            command.Parameters.AddWithValue("@classInfo", character.ClassInfo);
            command.Parameters.AddWithValue("@positionX", character.Position.X);
            command.Parameters.AddWithValue("@positionY", character.Position.Y);
            command.Parameters.AddWithValue("@height", character.Height);
            command.Parameters.AddWithValue("@trunk", character.Trunk);
            command.Parameters.AddWithValue("@leg", character.Leg);
            command.Parameters.AddWithValue("@body", character.Body);
            command.Parameters.AddWithValue("@level", character.Level);
            command.Parameters.AddWithValue("@experience", character.Experience);
            command.Parameters.AddWithValue("@strength", character.Strength);
            command.Parameters.AddWithValue("@agility", character.Agility);
            command.Parameters.AddWithValue("@constitution", character.Constitution);
            command.Parameters.AddWithValue("@intelligence", character.Intelligence);
            command.Parameters.AddWithValue("@luck", character.Luck);
            command.Parameters.AddWithValue("@status", character.Status);
            command.Parameters.AddWithValue("@creationTime", DateTime.UtcNow);
            command.Parameters.AddWithValue("@numericErrors", character.NumericErrors);
            command.Parameters.AddWithValue("@speedMove", character.SpeedMove);
            command.Parameters.AddWithValue("@firstLogin", character.FirstLogin);
            command.Parameters.AddWithValue("@currentHealth", character.CurrentHealth);
            command.Parameters.AddWithValue("@currentMana", character.CurrentMana);

            var characterId = Convert.ToInt32(await command.ExecuteScalarAsync());
            if(characterId <= 0) throw new Exception("Erro ao inserir personagem.");

            foreach(var equip in character.Equips.Where(e => e.ItemId > 0)) {
                Console.WriteLine($"Inserindo Equipamento -> Slot: {equip.Slot}, ItemId: {equip.ItemId}");
                await using var equipCommand = new MySqlCommand(
                    "INSERT INTO itens (ownerId, itemId, slot, slotType, app, identification, effectIndex1, effectValue1, effectIndex2, effectValue2, effectIndex3, effectValue3, minimalItemValue, maxItemValue, refine, `time`) " +
                    "VALUES (@ownerId, @itemId, @slot, @slotType, @app, @identification, @effectIndex1, @effectValue1, @effectIndex2, @effectValue2, @effectIndex3, @effectValue3, @minimalItemValue, @maxItemValue, @refine, @time);",
                    connection, transaction
                );

                equipCommand.Parameters.AddWithValue("@ownerId", characterId);
                equipCommand.Parameters.AddWithValue("@itemId", equip.ItemId);
                equipCommand.Parameters.AddWithValue("@slot", equip.Slot);
                equipCommand.Parameters.AddWithValue("@slotType", equip.SlotType);
                equipCommand.Parameters.AddWithValue("@app", equip.App);
                equipCommand.Parameters.AddWithValue("@identification", equip.Identification);
                equipCommand.Parameters.AddWithValue("@effectIndex1", 0);
                equipCommand.Parameters.AddWithValue("@effectValue1", 0);
                equipCommand.Parameters.AddWithValue("@effectIndex2", 0);
                equipCommand.Parameters.AddWithValue("@effectValue2", 0);
                equipCommand.Parameters.AddWithValue("@effectIndex3", 0);
                equipCommand.Parameters.AddWithValue("@effectValue3", 0);
                equipCommand.Parameters.AddWithValue("@minimalItemValue", equip.MinimalValue);
                equipCommand.Parameters.AddWithValue("@maxItemValue", equip.MaxValue);
                equipCommand.Parameters.AddWithValue("@refine", equip.Refine);
                equipCommand.Parameters.AddWithValue("@time", equip.Time);

                var rowsAffected = await equipCommand.ExecuteNonQueryAsync();
                if(rowsAffected == 0) {
                    Console.WriteLine("Erro ao inserir equipamento no banco de dados.");
                }
            }

            await transaction.CommitAsync();
            Console.WriteLine("Transação confirmada!");
            return true;
        }
        catch {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public static async Task<bool> VerifyIfCharacterNameExistsAsync(string name) {
        await using var connection = await DatabaseHandler.GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT 1 FROM characters WHERE name = @name LIMIT 1", connection);
        command.Parameters.AddWithValue("@name", name);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    public static async Task<bool> SaveCharacterNumericAsync(string characterName, string numeric, int numericErrors) {
        await using var connection = await DatabaseHandler.GetConnectionAsync();
        await using var command = new MySqlCommand("UPDATE characters SET numericToken = @numericToken, numericErrors = @numericErrors WHERE name = @name", connection);
        command.Parameters.AddWithValue("@numericToken", numeric);
        command.Parameters.AddWithValue("@numericErrors", numericErrors);
        command.Parameters.AddWithValue("@name", characterName);
        return await command.ExecuteNonQueryAsync() > 0;
    }
}

