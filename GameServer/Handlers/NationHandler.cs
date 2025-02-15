using GameServer.Core.Protocol;
using GameServer.Network;
using Shared.Core.Instance;

namespace GameServer.Handlers;

public class NationHandler : Singleton<NationHandler> {
    public List<Server> Servers { get; private set; }

    private readonly GameProtocol protocol = new();

    public NationHandler() {
        Servers = [];
    }

    public void LoadServers(ServersHandle config) {
        for(int i = 0; i < config.ServersCount; i++) {
            var server = new Server(config.ServersIP[i], 100, protocol, config.ServersName[i], config.ServersID[i]);

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