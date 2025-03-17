using GameServer.Core.Protocol;
using GameServer.Network;
using Shared.Core;

namespace GameServer.Core.Handlers.InGame;
public class NationHandler : Singleton<NationHandler> {
    public List<Server> ServersList { get; private set; }

    private readonly GameProtocol protocol = new();

    public NationHandler() {
        ServersList = [];
    }

    public void LoadServers(ServersHandle config) {
        for(int i = 0; i < config.ServersCount; i++) {
            var server = new Server(config.ServersIP[i], 100, protocol);

            server.StartAsync();

            if(server.IsStarted) {
                ServersList.Add(server);
                server.Name = config.ServersName[i];
                server.NationId = config.ServersID[i];
                Console.WriteLine($"Server[{server.NationId}]: [{server.Name}]");
            }
        }
    }

    public void StopServers() {
        foreach(var server in ServersList) {
            server.Stop();
        }
    }
}