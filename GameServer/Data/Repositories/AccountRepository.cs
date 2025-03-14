using GameServer.Model.Account;
using MySqlConnector;
using System.Data;

namespace GameServer.Data.Repositories;
public static class AccountRepository {

    // TO-DO: NÃO TA FUNCIONANDO!
    public static async Task<AccountEntitie> CheckLogin(string username, string password) {
        await using var connection = await DatabaseHandler.GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT * FROM accounts WHERE username = @username, password = @password LIMIT 1", connection);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password", password);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapAccount(reader) : null;
    }

    public static async Task<AccountEntitie> GetAccountByUsernameAsync(string username) {
        await using var connection = await DatabaseHandler.GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT * FROM accounts WHERE username = @username", connection);
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapAccount(reader) : null;
    }

    public static async Task<AccountEntitie> GetAccountByIdAsync(uint id) {
        await using var connection = await DatabaseHandler.GetConnectionAsync();
        await using var command = new MySqlCommand("SELECT 1 FROM accounts WHERE id = @id LIMIT 1", connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapAccount(reader) : null;
    }

    private static AccountEntitie MapAccount(MySqlDataReader reader) {
        return new AccountEntitie {
            Id = reader.GetUInt16("id"),
            Username = reader.GetString("username"),
            PasswordHash = reader.GetString("passwordHash"),
            Token = reader.GetString("token"),
            TokenCreationTime = reader.GetDateTime("tokenCreationTime"),
            AccountStatus = reader.GetByte("accountStatus"),
            BanDays = reader.GetByte("banDays"),
            Nation = reader.GetByte("nation"),
            AccountType = (AccountType)reader.GetInt32("accountType"),
            StorageGold = reader.IsDBNull("storageGold") ? 0 : reader.GetUInt32("storageGold"),
            Cash = reader.IsDBNull("cash") ? 0 : reader.GetUInt32("cash"),
            PremiumExpiration = reader.IsDBNull("premiumExpiration") ? string.Empty : reader.GetString("premiumExpiration"),
        };
    }
}
