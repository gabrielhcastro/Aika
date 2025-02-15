using MySqlConnector;
using System.Security.Cryptography;
using System.Text;

namespace TokenServer.Handlers;

public static class AuthHandlers {
    public static async Task<string> GetTokenAsync(string username, string passwordHash) {
        try {
            await using var connection = await DatabaseHandler.GetConnectionAsync();

            // Obtém a conta do banco de dados
            const string query = "SELECT id, passwordHash, accountStatus, banDays, tokenCreationTime FROM accounts WHERE username = @username";
            await using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);

            await using var reader = await command.ExecuteReaderAsync();
            if(!await reader.ReadAsync())
                return "0"; // Conta não encontrada

            var dbPasswordHash = reader.GetString("passwordHash");
            if(dbPasswordHash != passwordHash)
                return "-1"; // Senha incorreta

            int accountStatus = reader.GetInt32("accountStatus");
            int banDays = reader.GetInt32("banDays");
            DateTime tokenCreationTime = reader.GetDateTime("tokenCreationTime");

            if(accountStatus == 8) {
                if(banDays > 0 && DateTime.UtcNow > tokenCreationTime.AddDays(banDays)) {
                    // Remove o banimento
                    await using var updateCommand = new MySqlCommand(
                        "UPDATE accounts SET accountStatus = 0, banDays = 0 WHERE username = @username",
                        connection);
                    updateCommand.Parameters.AddWithValue("@username", username);
                    await updateCommand.ExecuteNonQueryAsync();
                    return "-22"; // Ban expirado
                }
                return "-8"; // Conta banida
            }

            if(accountStatus == 10)
                return "-10"; // Conta suspensa

            string newToken = GenerateToken();

            // Atualiza o token no banco
            await using var updateTokenCommand = new MySqlCommand(
                "UPDATE accounts SET token = @token, tokenCreationTime = NOW() WHERE username = @username",
                connection);
            updateTokenCommand.Parameters.AddWithValue("@token", newToken);
            updateTokenCommand.Parameters.AddWithValue("@username", username);
            await updateTokenCommand.ExecuteNonQueryAsync();

            Console.WriteLine("Token [{0}] criado para [{1}].", newToken, username);
            return newToken;
        }
        catch(Exception ex) {
            Console.WriteLine("Erro ao gerar token: {0}", ex.Message);
            return "-99";
        }
    }


    public static async Task<string> GetCharacterCountAsync(string username, string passwordHash) {
        try {
            await using var connection = await DatabaseHandler.GetConnectionAsync();

            const string accountQuery = "SELECT id, nation FROM accounts WHERE username = @username AND token = @token";
            await using var accountCommand = new MySqlCommand(accountQuery, connection);
            accountCommand.Parameters.AddWithValue("@username", username);
            accountCommand.Parameters.AddWithValue("@token", passwordHash);

            await using var reader = await accountCommand.ExecuteReaderAsync();
            if(!await reader.ReadAsync())
                return "0"; // Conta não encontrada ou token inválido

            int accountId = reader.GetInt32("id");
            int nation = reader.GetInt32("nation");
            reader.Close();

            // Conta o número de personagens
            const string charQuery = "SELECT COUNT(*) FROM characters WHERE ownerAccountId = @accountId";
            await using var charCommand = new MySqlCommand(charQuery, connection);
            charCommand.Parameters.AddWithValue("@accountId", accountId);

            int charCount = Convert.ToInt32(await charCommand.ExecuteScalarAsync());

            var infos = new StringBuilder();
            infos.Append($"CNT {charCount} 0 0 0 <br> ");
            infos.Append($"{nation} 0 0 0");

            return infos.ToString();
        }
        catch(Exception ex) {
            Console.WriteLine("Erro ao recuperar contagem de personagens: {0}", ex.Message);
            return "-99";
        }
    }

    public static async Task ResetFlag(string username, string passwordHash) {
        try {
            var _username = username;
            var token = passwordHash;

            await using var connection = await DatabaseHandler.GetConnectionAsync();

            const string accountQuery = "SELECT token, tokenCreationTime FROM accounts WHERE username = @username AND token = @token";
            await using var accountCommand = new MySqlCommand(accountQuery, connection);
            accountCommand.Parameters.AddWithValue("@username", (object)username);
            accountCommand.Parameters.AddWithValue("@token", token);

            await using var reader = await accountCommand.ExecuteReaderAsync();

            string newToken = GenerateToken();

            // Atualiza o token no banco
            await using var updateTokenCommand = new MySqlCommand(
                "UPDATE accounts SET token = @token, tokenCreationTime = NOW() WHERE username = @username",
                connection);
            updateTokenCommand.Parameters.AddWithValue("@token", newToken);
            updateTokenCommand.Parameters.AddWithValue("@username", username);
            await updateTokenCommand.ExecuteNonQueryAsync();

            Console.WriteLine("Token [{0}] criado para {1}.", newToken, username);
        }
        catch(Exception ex) {
            Console.WriteLine("Erro ao renovar token: {0}", ex.Message);
        }
    }

    public static async Task<string> CreateAccountAsync(string username, string passwordHash, int accountType) {
        const string _connectionString = "Server=localhost;Database=aika;User=root;Password=senha;Pooling=true;Min Pool Size=5;Max Pool Size=100";
        try {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Verifica se a conta já existe
            const string checkQuery = "SELECT COUNT(*) FROM accounts WHERE username = @username";
            await using var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@username", username);

            var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
            if(exists) return "0"; // Conta já existe

            // Query para inserir nova conta
            const string insertQuery = @"
                INSERT INTO accounts (username, password_hash, token, tokenCreationTime, accountStatus, 
                                     banDays, nation, accountType, storageGold, cash, premiumTime)
                VALUES (@username, @passwordHash, @token, @tokenCreationTime, @accountStatus, 
                        @banDays, @nation, @accountType, @storageGold, @cash, @premiumTime);
                SELECT LAST_INSERT_ID();"; // Retorna o ID da conta criada

            await using var command = new MySqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
            command.Parameters.AddWithValue("@token", Guid.NewGuid().ToString()); // Gera um token aleatório
            command.Parameters.AddWithValue("@tokenCreationTime", DateTime.UtcNow);
            command.Parameters.AddWithValue("@accountStatus", 1); // Status ativo
            command.Parameters.AddWithValue("@banDays", 0);
            command.Parameters.AddWithValue("@nation", 0); // Sem nação no início
            command.Parameters.AddWithValue("@accountType", accountType);
            command.Parameters.AddWithValue("@storageGold", 0);
            command.Parameters.AddWithValue("@cash", 0);
            command.Parameters.AddWithValue("@premiumTime", DBNull.Value); // Sem premium no início

            var accountId = await command.ExecuteScalarAsync();

            Console.WriteLine("Conta {0} criada com sucesso. ID: {1}", username, accountId);
            return accountId?.ToString() ?? "-99";
        }
        catch(Exception ex) {
            Console.WriteLine("Erro ao criar conta {0}\n{1}", username, ex);
            return "-99";
        }
    }

    //TO-DO: Implementar método para recuperar lista de servidores
    public static string GetPlayerCountPerServer() {
        var serverList = new string[64];
        for(int i = 0; i < serverList.Length; i++) {
            serverList[i] = "0";
        }
        return string.Join(" ", serverList);
    }


    private static string GenerateToken() {
        var md5Byte = MD5.HashData(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        var hash = new StringBuilder();
        foreach(var mByte in md5Byte) {
            hash.Append(mByte.ToString("x2"));
        }

        return hash.ToString();
    }
}