using GameServer.Handlers.Managers.Nation;
using GameServer.Models;

namespace GameServer;

class Program {
    static void Main(string[] args) {
        Console.WriteLine("Starting server...");

        ServerConfig config = new ServerConfig("AikaServer.ini");
        NationManager.Instance.LoadServers(config);

        Console.WriteLine("Press ENTER to stop the server.");
        Console.ReadLine();

        NationManager.Instance.StopServers();
        Console.WriteLine("Server stopped.");
    }
}
