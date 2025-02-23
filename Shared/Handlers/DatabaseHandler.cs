using MySqlConnector;
using Shared.Core.Instance;
using Shared.Models.Account;
using Shared.Models.Character;
using System.Data;
using System.Reflection.PortableExecutable;
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
        await using var command = new MySqlCommand("SELECT * FROM accounts WHERE username = @username", connection);
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapAccount(reader) : null;
    }

    public static async Task<AccountEntitie> GetAccountByIdAsync(uint id) {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT * FROM accounts WHERE id = @id", connection);
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
            characters.Add(MapCharacter(reader));
        }

        return characters;
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
    /// Registra o personagem na database.
    /// </summary>
    public static async Task<bool> CreateCharacterAsync(CharacterEntitie character,int accountId) {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand(
            "INSERT INTO characters (id, ownerAccountId, name, slot, classInfo, positionX, positionY, height, trunk, leg, body, level, experience," +
            "strength, agility, constitution, intelligence, luck, status, creationTime) " +
            "VALUES (@id, @ownerAccountId, @name, @slot, @classInfo, @positionX, @positionY, @height, @trunk, @leg, @body, @level, @experience," +
            "@strength, @agility, @constitution, @intelligence, @luck, @status, @creationTime);", connection);

        command.Parameters.AddWithValue("@id", character.Id);
        command.Parameters.AddWithValue("@ownerAccountId", accountId);
        command.Parameters.AddWithValue("@name", character.Name);
        command.Parameters.AddWithValue("@slot", character.Slot);
        command.Parameters.AddWithValue("@classInfo", character.ClassInfo);

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


        // Adicionar Item na tabela de itens e associar a conta
        // command.Parameters.AddWithValue("@hair", character.Hair);

        command.Parameters.AddWithValue("@positionX", character.PositionX);
        command.Parameters.AddWithValue("@positionY", character.PositionY);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    public static async Task<bool> VerifyIfCharacterNameExistsAsync(string name) {
        await using var connection = await GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT * FROM characters WHERE name = @name", connection);
        command.Parameters.AddWithValue("@name", name);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
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