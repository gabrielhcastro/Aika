using MySqlConnector;
using Shared.Core.Instance;
using Shared.Models.Account;
using Shared.Models.Character;
using Shared.Models.Item;
using System.Data;
using System.Data.Common;
using System.Reflection.PortableExecutable;
using System.Transactions;
using System.Xml.Linq;

namespace Shared.Handlers;
public class DatabaseHandler : Singleton<DatabaseHandler> {
    private static readonly string _connectionString = "Server=localhost;Port=3306;Database=aikaria;User=root;Password=Jose2904.;Pooling=true;Min Pool Size=5;Max Pool Size=100;";

    public DatabaseHandler() { }

    /// <summary>
    /// Obtém a conexão MySql de forma assíncrona.
    /// </summary>
    public static async Task<MySqlConnection> GetConnectionAsync() {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    // TO-DO: USAR O PASSWORD HASH?
    /// <summary>
    /// Obtém uma conta pelo nome de usuário.
    /// </summary>
    public static async Task<AccountEntitie> GetAccountByUsernameAsync(string username) {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT * FROM accounts WHERE username = @username LIMIT 1", connection);
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapAccount(reader) : null;
    }

    /// <summary>
    /// Deixa ai vai que um dia precisa né...
    /// </summary>
    public static async Task<AccountEntitie> GetAccountByIdAsync(uint id) {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT 1 FROM accounts WHERE id = @id LIMIT 1", connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapAccount(reader) : null;
    }

    /// <summary>
    /// Recupera personagens através da ID da conta.
    /// </summary>
    public static async Task<List<CharacterEntitie>> GetCharactersByAccountIdAsync(int accountId) {
        List<CharacterEntitie> characters = [];

        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT * FROM characters WHERE ownerAccountId = @accountId", connection);
        command.Parameters.AddWithValue("@accountId", accountId);

        await using var reader = await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            var character = MapCharacter(reader);

            character.Equips = await GetCharacterEquips(character.Id);

            characters.Add(character);
        }

        return characters;
    }

    public static async Task<List<ItemEntitie>> GetCharacterEquips(uint characterId) {
        List<ItemEntitie> equips = new();

        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT * FROM itens WHERE ownerId = @characterId AND slotType = 1", connection);
        command.Parameters.AddWithValue("@characterId", characterId);

        await using var reader = await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            ItemEntitie equip = new() {
                OwnerId = reader.GetUInt32("ownerId"),
                Slot = reader.GetByte("slot"),
                SlotType = reader.GetByte("slotType"),
                Id = reader.GetUInt32("itemId"),
                App = reader.GetUInt32("app"),
                MinimalValue = reader.GetUInt32("minimalItemValue"),
                MaxValue = reader.GetUInt32("maxItemValue"),
                Refine = reader.GetUInt16("refine"),
                Time = reader.GetUInt32("time")
            };

            equips.Add(equip);
        }

        return equips;
    }


    /// <summary>
    /// Mapeia a conta.
    /// </summary>
    private static AccountEntitie MapAccount(MySqlDataReader reader) {
        return new AccountEntitie {
            Id = reader.GetInt32("id"),
            Username = reader.GetString("username"),
            PasswordHash = reader.GetString("passwordHash"),
            Token = reader.GetString("token"),
            TokenCreationTime = reader.GetDateTime("tokenCreationTime"),
            AccountStatus = reader.GetInt32("accountStatus"),
            BanDays = reader.GetInt32("banDays"),
            Nation = reader.GetInt32("nation"),
            AccountType = (AccountType)reader.GetInt32("accountType"),
            StorageGold = reader.IsDBNull("storageGold") ? 0 : reader.GetInt32("storageGold"),
            Cash = reader.IsDBNull("cash") ? 0 : reader.GetInt32("cash"),
            PremiumExpiration = reader.IsDBNull("premiumExpiration") ? string.Empty : reader.GetString("premiumExpiration"),
        };
    }

    /// <summary>
    /// Mapeia os personagens de uma conta.
    /// </summary>
    private static CharacterEntitie MapCharacter(MySqlDataReader reader) {
        return new CharacterEntitie {
            Id = reader.GetUInt32("id"),
            OwnerAccountId = reader.GetUInt32("ownerAccountId"),
            Name = reader.GetString("name"),
            Slot = reader.GetUInt32("slot"),
            NumericToken = reader.IsDBNull("numericToken") ? string.Empty : reader.GetString("numericToken"),
            NumericErrors = reader.IsDBNull("numericErrors") ? 0 : reader.GetUInt32("numericErrors"),
            Deleted = reader.GetByte("deleted"),
            SpeedMove = reader.IsDBNull("speedMove") ? 0 : reader.GetUInt32("speedMove"),
            Rotation = reader.IsDBNull("rotation") ? 0 : reader.GetUInt32("rotation"),
            LastLogin = reader.IsDBNull("lastLogin") ? "N/A" : reader.GetString("lastLogin"),
            PlayerKill = reader.GetByte("playerKill"),
            ClassInfo = reader.GetUInt32("classInfo"),
            FirstLogin = reader.IsDBNull("firstLogin") ? 0 : reader.GetUInt32("firstLogin"),
            Strength = reader.GetUInt32("strength"),
            Agility = reader.GetUInt32("agility"),
            Intelligence = reader.GetUInt32("intelligence"),
            Constitution = reader.GetUInt32("constitution"),
            Luck = reader.GetUInt32("luck"),
            Status = reader.GetUInt32("status"),
            Height = reader.GetUInt32("height"),
            Trunk = reader.GetUInt32("trunk"),
            Leg = reader.GetUInt32("leg"),
            Body = reader.GetUInt32("body"),
            CurrentHealth = reader.IsDBNull("currentHealth") ? 0 : reader.GetUInt32("currentHealth"),
            CurrentMana = reader.IsDBNull("currentMana") ? 0 : reader.GetUInt32("currentMana"),
            Honor = reader.IsDBNull("honor") ? 0 : reader.GetUInt32("honor"),
            KillPoint = reader.IsDBNull("killPoint") ? 0 : reader.GetUInt32("killPoint"),
            Infamia = reader.IsDBNull("infamia") ? 0 : reader.GetUInt32("infamia"),
            SkillPoint = reader.IsDBNull("skillPoint") ? 0 : reader.GetUInt32("skillPoint"),
            Experience = reader.GetUInt64("experience"),
            Level = reader.GetUInt32("level"),
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
            SavedPositionY = reader.IsDBNull("savedPositionY") ? 0 : reader.GetUInt32("savedPositionY")
        };
    }

    /// <summary>
    /// Registra o personagem.
    /// </summary>
    public static async Task<bool> CreateCharacterAsync(CharacterEntitie character, int accountId) {
        await using var connection = await GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try {
            await using var command = new MySqlCommand(
                "INSERT INTO characters (ownerAccountId, name, slot, classInfo, positionX, positionY, height, trunk, leg, body, level, experience," +
                "strength, agility, constitution, intelligence, luck, status, creationTime, numericErrors, speedMove, firstLogin) " +
                "VALUES (@ownerAccountId, @name, @slot, @classInfo, @positionX, @positionY, @height, @trunk, @leg, @body, @level, @experience," +
                "@strength, @agility, @constitution, @intelligence, @luck, @status, @creationTime, @numericErrors, @speedMove, @firstLogin); " +
                "SELECT LAST_INSERT_ID();",
                connection, transaction
            );

            command.Parameters.AddWithValue("@ownerAccountId", accountId);
            command.Parameters.AddWithValue("@name", character.Name);
            command.Parameters.AddWithValue("@slot", character.Slot);
            command.Parameters.AddWithValue("@classInfo", character.ClassInfo);
            command.Parameters.AddWithValue("@positionX", character.PositionX);
            command.Parameters.AddWithValue("@positionY", character.PositionY);
            command.Parameters.AddWithValue("@height", 7);
            command.Parameters.AddWithValue("@trunk", 119);
            command.Parameters.AddWithValue("@leg", 119);
            command.Parameters.AddWithValue("@body", 0);
            command.Parameters.AddWithValue("@level", 0);
            command.Parameters.AddWithValue("@experience", 0);
            command.Parameters.AddWithValue("@strength", 0);
            command.Parameters.AddWithValue("@agility", 0);
            command.Parameters.AddWithValue("@constitution", 0);
            command.Parameters.AddWithValue("@intelligence", 0);
            command.Parameters.AddWithValue("@luck", 0);
            command.Parameters.AddWithValue("@status", 0);
            command.Parameters.AddWithValue("@creationTime", DateTime.UtcNow);
            command.Parameters.AddWithValue("@numericErrors", character.NumericErrors);
            command.Parameters.AddWithValue("@speedMove", character.SpeedMove);
            command.Parameters.AddWithValue("@firstLogin", character.FirstLogin);

            var characterId = Convert.ToInt32(await command.ExecuteScalarAsync());
            if(characterId <= 0) throw new Exception("Erro ao inserir personagem.");

            foreach(var equip in character.Equips.Where(e => e.Id > 0)) {
                await using var equipCommand = new MySqlCommand(
                    "INSERT INTO itens (ownerId, slotType, slot, itemId, app, identification, effectIndex1, effectValue1, effectIndex2, effectValue2, effectIndex3, effectValue3, minimalItemValue, maxItemValue, refine, `time`) " +
                    "VALUES (@ownerId, @slotType, @slot, @itemId, @app, @identification, @effectIndex1, @effectValue1, @effectIndex2, @effectValue2, @effectIndex3, @effectValue3, @minimalItemValue, @maxItemValue, @refine, @time);",
                    connection, transaction
                );

                equipCommand.Parameters.AddWithValue("@ownerId", characterId);
                equipCommand.Parameters.AddWithValue("@slot", equip.Slot);
                equipCommand.Parameters.AddWithValue("@slotType", 1);
                equipCommand.Parameters.AddWithValue("@itemId", equip.Id);
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

                await equipCommand.ExecuteNonQueryAsync();
            }

            foreach(var item in character.Itens.Where(i => i.Id > 0)) {
                await using var itemCommand = new MySqlCommand(
                    "INSERT INTO itens (ownerId, slotType, slot, itemId, app, identification, effectIndex1, effectValue1, effectIndex2, effectValue2, effectIndex3, effectValue3, minimalItemValue, maxItemValue, refine, `time`) " +
                    "VALUES (@ownerId, @slotType, @slot, @itemId, @app, @identification, @effectIndex1, @effectValue1, @effectIndex2, @effectValue2, @effectIndex3, @effectValue3, @minimalItemValue, @maxItemValue, @refine, @time);",
                    connection, transaction
                );

                itemCommand.Parameters.AddWithValue("@ownerId", characterId);
                itemCommand.Parameters.AddWithValue("@slot", item.Slot);
                itemCommand.Parameters.AddWithValue("@slotType", 2);
                itemCommand.Parameters.AddWithValue("@itemId", item.Id);
                itemCommand.Parameters.AddWithValue("@app", item.App);
                itemCommand.Parameters.AddWithValue("@identification", item.Identification);
                itemCommand.Parameters.AddWithValue("@effectIndex1", 0);
                itemCommand.Parameters.AddWithValue("@effectValue1", 0);
                itemCommand.Parameters.AddWithValue("@effectIndex2", 0);
                itemCommand.Parameters.AddWithValue("@effectValue2", 0);
                itemCommand.Parameters.AddWithValue("@effectIndex3", 0);
                itemCommand.Parameters.AddWithValue("@effectValue3", 0);
                itemCommand.Parameters.AddWithValue("@minimalItemValue", item.MinimalValue);
                itemCommand.Parameters.AddWithValue("@maxItemValue", item.MaxValue);
                itemCommand.Parameters.AddWithValue("@refine", item.Refine);
                itemCommand.Parameters.AddWithValue("@time", item.Time);

                await itemCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return true;
        }
        catch {
            await transaction.RollbackAsync();
            return false;
        }
    }

    /// <summary>
    /// Verifica se o nome do personagem já existe.
    /// </summary>
    public static async Task<bool> VerifyIfCharacterNameExistsAsync(string name) {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT 1 FROM characters WHERE name = @name LIMIT 1", connection);
        command.Parameters.AddWithValue("@name", name);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// Salva ou atualiza a numérica do personagem.
    /// </summary>
    public static async Task<bool> SaveCharacterNumeric(string characterName, string numeric, int numericErrors) {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("UPDATE characters SET numericToken = @numericToken, numericErrors = @numericErrors WHERE name = @name", connection);
        command.Parameters.AddWithValue("@numericToken", numeric);
        command.Parameters.AddWithValue("@numericErrors", numericErrors);
        command.Parameters.AddWithValue("@name", characterName);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    //private static void SetInitialBullets(Player player, int slotIndex, int classCategory) {
    //    if(classCategory == 2) {
    //        SetBullet(player, slotIndex, 4615);
    //    }
    //    else if(classCategory == 3) {
    //        SetBullet(player, slotIndex, 4600);
    //    }
    //}

    //private static void SetBullet(Player player, int slotIndex, int bulletId) {
    //    player.Account.Characters[slotIndex].Base.Equip[15].Index = bulletId;
    //    player.Account.Characters[slotIndex].Base.Equip[15].APP = bulletId;
    //    player.Account.Characters[slotIndex].Base.Equip[15].Refi = 1000;
    //    player.Account.Characters[slotIndex].Base.Inventory[5].Index = bulletId;
    //    player.Account.Characters[slotIndex].Base.Inventory[5].APP = bulletId;
    //    player.Account.Characters[slotIndex].Base.Inventory[5].Refi = 1000;
    //    player.Account.Characters[slotIndex].Base.Inventory[6].Index = bulletId;
    //    player.Account.Characters[slotIndex].Base.Inventory[6].APP = bulletId;
    //    player.Account.Characters[slotIndex].Base.Inventory[6].Refi = 1000;
    //}
}