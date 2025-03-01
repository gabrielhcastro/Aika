using MySqlConnector;
using Shared.Core;

namespace GameServer.Data;
public class DatabaseHandler : Singleton<DatabaseHandler> {
    private static readonly string _connectionString = "Server=localhost;Port=3306;Database=aikaria;User=root;Password=Jose2904.;Pooling=true;Min Pool Size=5;Max Pool Size=100;";

    public DatabaseHandler() { }

    public static async Task<MySqlConnection> GetConnectionAsync() {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}