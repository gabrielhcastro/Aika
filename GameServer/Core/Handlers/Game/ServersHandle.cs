using System.Net;

namespace GameServer.Core.Handlers.InGame;
public class ServersHandle {
    public int ServersCount { get; private set; }
    public List<string> ServersName { get; private set; } = [];
    public List<IPEndPoint> ServersIP { get; private set; } = [];
    public List<byte> ServersID { get; private set; } = [];

    //TO-DO: Load from database
    public ServersHandle(string configPath) {
        _ = configPath;
        //var ini = new IniFile(configPath);
        ServersCount = 0;

        ServersName = [];
        ServersIP = [];
        ServersID = [];

        LoadServers();

        Console.WriteLine($"{ServersCount} servers config loaded.");
    }

    public void LoadServers() {
        ServersName.Add("Login");
        ServersIP.Add(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8831));
        ServersID.Add(0);
        ServersCount++;

        ServersName.Add("Elsinore");
        ServersIP.Add(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8822));
        ServersID.Add(1);
        ServersCount++;

        ServersName.Add("Odeon");
        ServersIP.Add(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 8822));
        ServersID.Add(2);
        ServersCount++;

        ServersName.Add("Tiberica");
        ServersIP.Add(new IPEndPoint(IPAddress.Parse("127.0.0.3"), 8822));
        ServersID.Add(3);
        ServersCount++;
    }
}