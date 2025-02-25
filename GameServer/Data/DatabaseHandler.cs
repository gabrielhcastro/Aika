using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Model.Item;
using MySqlConnector;
using Shared.Core;
using Shared.Models.Account;
using System.Data;

namespace GameServer.Data;
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
}