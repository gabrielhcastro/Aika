using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace TokenServer.Handlers;

public class DatabaseHandler : DbContext {
    private static readonly string ConnectionString =
            "Server=localhost;Port=3306;Database=aikaria;User=root;Password=Jose2904;";

    public static async Task<MySqlConnection> GetConnectionAsync() {
        var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}
