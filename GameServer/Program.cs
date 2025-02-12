using GameServer.Protocol;
using Shared.Network;
using System.Net;

namespace GameServer;

class Program {
    static void Main(string[] args) {
        var endpoint = new IPEndPoint(IPAddress.Any, 8831);
        var protocol = new GameProtocol();
        var server = new Server(endpoint, 100, protocol);

        Console.WriteLine("Starting server...");
        server.Start();

        Console.WriteLine("Press ENTER to stop the server.");
        Console.ReadLine();

        server.Stop();
        Console.WriteLine("Server stopped.");
    }
}
