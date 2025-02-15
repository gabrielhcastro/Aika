using GameServer.Handlers;
using Shared;

namespace GameServer;

class Program {

    static void Main(string[] args) {
        _ = args;
        Console.WriteLine("Starting server...");

        ServersHandle config = new("AikaServer.ini");
        NationHandler.Instance.LoadServers(config);

        Console.WriteLine("Press ENTER to stop the server.");
        Console.ReadLine();

        NationHandler.Instance.StopServers();
        Console.WriteLine("Server stopped.");
    }
}
