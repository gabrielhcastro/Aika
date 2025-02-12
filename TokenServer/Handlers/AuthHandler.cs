using System.Security.Cryptography;
using System.Text;
using TokenServer.Data;
using TokenServer.Models.Entities;

namespace TokenServer.Handlers;

public static class AuthHandlers {
    private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("AuthHandlers");

    public static string GetToken(string id, string pw) {
        try {
            var username = id;
            var password = pw;

            using var context = new ApplicationDbContext();
            var account = context.Accounts.FirstOrDefault(a => a.Username == username);

            if(account == null)
                return "0";

            if(account.Password != password)
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

            var token = GenerateToken(password);
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

    public static string GetCharacterCount(string id, string pw) {
        try {
            var username = id;
            var password = pw;

            using var context = new ApplicationDbContext();
            var account = context.Accounts.FirstOrDefault(a => a.Username == username);

            if(account == null)
                return "0";

            if(account.Token != password)
                return "-1";

            var charCount = 0; //TO-DO: Recuperar contagem de personagens

            var infos = new StringBuilder();
            infos.AppendLine("CNT " + charCount + " 0 0 0");
            infos.AppendLine("<br>");
            infos.AppendLine(account.Nation + " 0 0 0");

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

    public static string CreateAccount(Dictionary<string, string> parameters) {
        try {
            var username = parameters["id"];
            var password = parameters["pw"];
            var accountType = (AccountType)int.Parse(parameters["acctype"]);

            using var context = new ApplicationDbContext();
            if(context.Accounts.Any(a => a.Username == username))
                return "0";

            var account = new Account {
                Username = username,
                Password = password,
                AccountType = accountType,
                TokenCreationTime = DateTime.Now
            };

            context.Accounts.Add(account);
            context.SaveChanges();

            _logger.LogInformation("Conta {Username} criada com sucesso.", username);
            return account.Id.ToString();
        }
        catch(Exception ex) {
            _logger.LogError("Erro ao criar conta: {Message}", ex.Message);
            return "-99";
        }
    }

    //TO-DO: Implementar método para recuperar lista de servidores
    public static string GetServerList() {
        var infos = new StringBuilder();

        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("0");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("0");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("0");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");
        infos.AppendLine("-1");

        return infos.ToString();
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