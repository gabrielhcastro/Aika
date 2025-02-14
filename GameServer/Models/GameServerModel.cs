namespace GameServer.Models;

public class GameServerModel {
    public string Name { get; set; }
    public string IP { get; set; }
    public int ChannelId { get; set; }
    public int NationID { get; set; }

    public bool StartServer() {
        if(string.IsNullOrEmpty(IP) || IP == "0.0.0.0") {
            Console.WriteLine($"Servidor [{Name}] não iniciado (IP inválido)");
            return false;
        }

        Console.WriteLine($"Servidor {Name} iniciado no IP [{IP}], Canal [{ChannelId}], Nação [{NationID}]");
        return true;
    }
}
