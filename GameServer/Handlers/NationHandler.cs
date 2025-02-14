using GameServer.Core.Instance;
using GameServer.Core.Protocol;
using GameServer.Network;

namespace GameServer.Handlers;

public class NationHandler : Singleton<NationHandler> {
    public List<Server> Servers { get; private set; }
    GameProtocol protocol = new GameProtocol();

    public NationHandler() {
        Servers = [];
    }

    public void LoadServers(ServersHandle config) {
        for(int i = 0; i < config.ServerCount; i++) {
            var server = new Server(config.ServerIPs[i], 100, protocol, config.ServerNames[i], config.NationIDs[i]);

            server.Start();

            if(server.IsStarted) {
                Servers.Add(server);
                Console.WriteLine($"Servidor: {server.Name}, Index: {server.NationId}");
            }
        }
    }

    public void StopServers() {
        foreach(var server in Servers) {
            server.Stop();
        }
    }
}