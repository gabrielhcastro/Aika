using MySqlConnector;
using System.Security.Cryptography;
using System.Text;
using TokenServer.Data;

namespace TokenServer.Handlers;

public static class AuthHandlers {
    private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("AuthHandlers ");

    public static string GetToken(string accountId, string passwordHash) {
        try {
            var username = accountId;
            var _passwordHash = passwordHash;

            using var context = new ApplicationDbContext();
            var account = context.Accounts.FirstOrDefault(a => a.Username == username);

            if(account == null)
                return "0";

            if(account.PasswordHash != _passwordHash)
                return "-1";

            if(account.AccountStatus == 8) {
                if(account.BanDays > 0 && DateTime.Now > account.TokenCreationTime.AddDays(account.BanDays)) {
                    account.AccountStatus = 0;
                    account.BanDays = 0;
                    context.SaveChanges();
                    return "-22";
                }
                return "-8";
            }

            if(account.AccountStatus == 10)
                return "-10";

            var token = GenerateToken(_passwordHash);
            account.Token = token;
            account.TokenCreationTime = DateTime.Now;
            context.SaveChanges();

            _logger.LogInformation("Token [{Token}] criado por {Username}.", token, username);
            return token;
        }
        catch(Exception ex) {
            _logger.LogError("Erro ao gerar token: {Message}", ex.Message);
            return "-99";
        }
    }

    public static string GetCharacterCount(string accountId, string passwordHash) {
        try {
            var username = accountId;
            var password = passwordHash;

            using var context = new ApplicationDbContext();
            var account = context.Accounts.FirstOrDefault(a => a.Username == username);

            if(account == null)
                return "0";

            if(account.Token != password)
                return "-1";

            var charCount = 0; //TO-DO: Recuperar contagem de personagens

            var infos = new StringBuilder();
            infos.Append("CNT " + charCount + " 0 0 0 ");
            infos.Append("<br> ");
            infos.Append(account.Nation + " 0 0 0 ");

            return infos.ToString();
        }
        catch(Exception ex) {
            _logger.LogError("Erro ao recuperar contagem de personagens: {Message}", ex.Message);
            return "-99";
        }
    }

    public static string ResetFlag(string id, string token) {
        try {
            var Username = id;
            var Token = token;

            using var context = new ApplicationDbContext();
            var account = context.Accounts.FirstOrDefault(a => a.Username == Username);

            if(account == null)
                return "0";

            if(account.Token != Token)
                return "-1";

            account.TokenCreationTime = DateTime.Now;
            context.SaveChanges();

            _logger.LogInformation("Token renovado do usuário: {Username}.", Username);
            return account.Token;
        }
        catch(Exception ex) {
            _logger.LogError("Erro ao renovar token: {Message}", ex.Message);
            return "-99";
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

            _logger.LogInformation("Conta {Username} criada com sucesso. ID: {AccountId}", username, accountId);
            return accountId?.ToString() ?? "-99";
        }
        catch(Exception ex) {
            _logger.LogError(ex, "Erro ao criar conta {Username}", username);
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


    private static string GenerateToken(string password) {
        var md5Byte = MD5.HashData(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        var hash = new StringBuilder();
        foreach(var mByte in md5Byte) {
            hash.Append(mByte.ToString("x2"));
        }

        return hash.ToString();
    }
}