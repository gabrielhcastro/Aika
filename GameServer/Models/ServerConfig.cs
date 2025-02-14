using System.Net;

namespace GameServer.Models;

public class ServerConfig {
    public int ServerCount { get; private set; }
    public List<string> ServerNames { get; private set; }
    public List<IPEndPoint> ServerIPs { get; private set; }
    public List<byte> NationIDs { get; private set; }

    //TO-DO: Load from config file
    public ServerConfig(string configPath) {
        //var ini = new IniFile(configPath);
        ServerCount = 0;

        ServerNames = new List<string>();
        ServerIPs = new List<IPEndPoint>();
        NationIDs = new List<byte>();


        ServerNames.Add("Login");
        ServerIPs.Add(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8831));
        NationIDs.Add(0);
        ServerCount++;

        ServerNames.Add("Elsinore");
        ServerIPs.Add(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8822));
        NationIDs.Add(1);
        ServerCount++;

        ServerNames.Add("Odeon");
        ServerIPs.Add(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 8822));
        NationIDs.Add(2);
        ServerCount++;

        ServerNames.Add("Tiberica");
        ServerIPs.Add(new IPEndPoint(IPAddress.Parse("127.0.0.3"), 8822));
        NationIDs.Add(3);
        ServerCount++;
        //for(int i = 0; i < ServerCount; i++) {
        //    ServerNames.Add("Server_01, Name, Elsinore");
        //    ServerIPs.Add("Server_01, IP, 127.0.0.1");
        //    NationIDs.Add(1);
        //    NationIndex.Add(0);
        //}

        Console.WriteLine($"Configuração carregada com {ServerCount} servidores.");
    }
}
