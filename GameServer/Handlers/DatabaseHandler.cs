using GameServer.Core.Instance;
using GameServer.GameLogic.Account;
using GameServer.Models;
using MySqlConnector;
using System.Data;

namespace GameServer.Handlers;

public class DatabaseHandler : Singleton<DatabaseHandler> {
    private static readonly string _connectionString = "Server=localhost;Port=3306;Database=aikaria;User=root;Password=Jose2904.;";

    private MySqlConnection _persistentConnection;
    private MySqlConnection GetConnection() => _persistentConnection;

    public DatabaseHandler() { }

    public void Initialize() {
        _persistentConnection = new MySqlConnection(_connectionString);
        _persistentConnection.Open();
    }

    /// <summary>
    /// Obtém uma conta pelo nome de usuário.
    /// </summary>
    public async Task<AccountEntitie> GetAccountByUsernameAsync(string username) {
        await using var command = new MySqlCommand("SELECT * FROM accounts WHERE username = @username", GetConnection());
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapAccount(reader) : null;
    }

    /// <summary>
    /// Obtém os personagens de uma conta pelo id da conta.
    /// </summary>
    public async Task<List<CharacterEntitie>> GetCharactersByAccountIdAsync(int accountId) {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = "SELECT * FROM characters WHERE ownerAccountId = @accountId";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@accountId", accountId);

        using var reader = await command.ExecuteReaderAsync();
        List<CharacterEntitie> characters = [];

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
            LoggedTime = reader.IsDBNull("loggedTime") ? 0 : reader.GetUInt32("loggedTime"),
            PlayerKill = reader.GetByte("playerKill"),
            ClassInfo = reader.GetUInt32("classInfo"),
            FirstLogin = reader.IsDBNull("firstLogin") ? 0 : reader.GetUInt32("firstLogin"),
            Strength = reader.GetUInt32("strength"),
            Agility = reader.GetUInt32("agility"),
            Intelligence = reader.GetUInt32("intelligence"),
            Constitution = reader.GetUInt32("constitution"),
            Luck = reader.GetUInt32("luck"),
            Status = reader.GetUInt32("status"),
            Altura = reader.GetUInt32("altura"),
            Tronco = reader.GetUInt32("tronco"),
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
}