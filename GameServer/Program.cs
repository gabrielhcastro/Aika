using GameServer.Handlers;

namespace GameServer;

class Program {

    static void Main(string[] args) {
        Console.WriteLine("Starting server...");

        ServersHandle config = new ServersHandle("AikaServer.ini");
        NationHandler.Instance.LoadServers(config);
        DatabaseHandler.Instance.Initialize();

        Console.WriteLine("Press ENTER to stop the server.");
        Console.ReadLine();

        NationHandler.Instance.StopServers();
        Console.WriteLine("Server stopped.");
    }
}
