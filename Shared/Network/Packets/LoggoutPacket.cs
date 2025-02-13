using Shared.Network.packet;

namespace Shared.Network.Packets;

public class LoginResponsePacket {
    private readonly PacketHandler _packet;

    public LoginResponsePacket(PacketHandler packet) {
        _packet = packet;
    }

    public void Process(Session session) {
        Console.WriteLine($"Jogador {session.Ip} desconectado.");
        session.Close();
    }
}
