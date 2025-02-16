using MySqlConnector;
using Shared.Core.Instance;
using Shared.Models.Account;
using Shared.Models.Character;
using System.Data;

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

    /// <summary>
    /// Obtém os personagens de uma conta pelo id da conta.
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

    //public static async Task<bool> CreateCharacterAsync(AccountEntitie account) {
    //    if(!account.VerifyAmount(packet.Name)) {
    //        account.SendClientMessage("Você já tem 3 personagens.", 16, 0, 1);
    //        return false;
    //    }

    //    if(!Functions.IsLetter(packet.Name)) {
    //        account.SendClientMessage("Você só pode usar caracteres alfanuméricos.", 16, 0, 1);
    //        return false;
    //    }

    //    if(packet.Name.Length > 14) {
    //        account.SendClientMessage("Limitado a 14 caracteres apenas.", 16, 0, 1);
    //        return false;
    //    }

    //    if(await account.NameExistsAsync(packet.Name)) {
    //        account.SendClientMessage("Já existe um personagem com esse nome.", 16, 0, 1);
    //        return false;
    //    }

    //    if(packet.SlotIndex > 2) {
    //        account.SendClientMessage("SLOT_ERROR", 16, 0, 1);
    //        return false;
    //    }

    //    if(packet.ClassIndex < 10) {
    //        account.SendClientMessage("class_id error, try to create your toon again.", 16, 0, 1);
    //        return false;
    //    }

    //    int classeChar = GetClassCategory(10);

    //    if(packet.Cabelo < 7700 || packet.Cabelo > 7731)
    //        return false;

    //    // Move atributos iniciais para a database ao criar o personagem
    //    account.Characters[packet.SlotIndex] = InitialAccounts[classeChar];
    //    account.Characters[packet.SlotIndex].Base.Equip[0].Index = packet.ClassIndex;
    //    account.Characters[packet.SlotIndex].Base.Equip[1].Index = packet.Cabelo;
    //    account.Characters[packet.SlotIndex].Base.Inventory[60].Index = 5300;
    //    account.Header.Storage.Items[80].Index = 5310;

    //    SetInitialBullets(account, packet.SlotIndex, classeChar);

    //    account.Characters[packet.SlotIndex].Base.CreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    //    account.Characters[packet.SlotIndex].Base.Name = packet.Name;

    //    SetCharacterPosition(account, packet.SlotIndex, packet.Local);

    //    Logger.Write($"{account.Account.Header.Username} criou um novo personagem [{packet.Name}].", LogType.ConnectionsTraffic);

    //    if(!await account.SaveCreatedCharAsync(packet.Name, packet.SlotIndex)) {
    //        account.Account.Characters[packet.SlotIndex] = new CharacterEntitie();
    //        account.SendCharList();
    //        return false;
    //    }

    //    await CheckReferralBonusAsync(account);

    //    account.SendCharList();
    //    return true;
    //}

    //private static int GetClassCategory(int classIndex) {
    //    if(classIndex >= 10 && classIndex <= 19) return 0; // Warrior
    //    if(classIndex >= 20 && classIndex <= 29) return 1; // Templar
    //    if(classIndex >= 30 && classIndex <= 39) return 2; // Att
    //    if(classIndex >= 40 && classIndex <= 49) return 3; // Dual
    //    if(classIndex >= 50 && classIndex <= 59) return 4; // Mage
    //    if(classIndex >= 60 && classIndex <= 69) return 5; // Cleric
    //    return -1;
    //}

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

    //private static void SetCharacterPosition(Player player, int slotIndex, int local) {
    //    switch(local) {
    //        case 0:
    //        player.Account.Characters[slotIndex].LastPos = new Position(3450, 690);
    //        break;
    //        case 1:
    //        player.Account.Characters[slotIndex].LastPos = new Position(3470, 935);
    //        break;
    //    }
    //}

    //private static async Task CheckReferralBonusAsync(Player player) {
    //    using var connection = await DatabaseHandler.GetConnectionAsync();
    //    using var command = new MySqlCommand(
    //        "SELECT COALESCE(av.referrer, '') FROM account_validate av INNER JOIN accounts a ON a.mail = av.email WHERE a.id = @accountId",
    //        connection);
    //    command.Parameters.AddWithValue("@accountId", player.Account.Header.AccountId);

    //    using var reader = await command.ExecuteReaderAsync();
    //    if(await reader.ReadAsync() && !string.IsNullOrEmpty(reader.GetString(0))) {
    //        ItemFunctions.PutItemOnEvent(player, 4357, 500);
    //    }
    //}
}