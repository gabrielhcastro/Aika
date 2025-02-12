using ClientServer.Protocol;
using Shared.Network;
using System.Net;

namespace GameServer;

class Program {
    static void Main(string[] args) {
        var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8831);
        var protocol = new GameProtocol();
        var client = new Client(endpoint, protocol);

        Console.WriteLine("Connecting to the server...");
        client.Start();

        Console.WriteLine("Press ENTER to disconnect.");
        Console.ReadLine();

        client.Stop();
        Console.WriteLine("Client stopped.");
    }
}
