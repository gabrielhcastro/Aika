using Shared.Network.packet;

namespace Shared.Network.Packets;
public class LoginPacket {
    private readonly PacketHandler _packet;

    public LoginPacket(PacketHandler packet) {
        _packet = packet;
    }

    public void Process(Session session) {
        var username = _packet.ReadString(20);
        var password = _packet.ReadString(20);

        Console.WriteLine($"Usuário: {username}, Senha: {password}");

        var response = new PacketHandler();
        response.Write((ushort)0x0082);
        session.SendPacket(response.GetBytes());
        session.SendPacket(_packet);
    }
}