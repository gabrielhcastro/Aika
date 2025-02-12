using Shared.Network;
using Shared.Network.Encryption;
using Shared.Network.Stream;
using Shared.Protocol;

namespace GameServer.Protocol;

public class GameProtocol : BaseProtocol {
    public override void OnConnect(Session session) {
        Console.WriteLine($"Player connected from {session.Ip}");
    }

    public override void OnDisconnect(Session session) {
        Console.WriteLine($"Player disconnected from {session.Ip}");
    }

    public override void OnReceive(Session session, byte[] buff, int bytes) {
        if(buff == null || buff.Length < 4) {
            Console.WriteLine("Invalid packet received.");
            return;
        }

        //TO-DO: Better way to handle this
        if(buff.Length > 2 && buff[0] == 0x11 && buff[1] == 0xF3) {
            var newBuff = new byte[buff.Length - 4];
            Array.Copy(buff, 4, newBuff, 0, buff.Length - 4);
            buff = newBuff;
        }

        var packet = new PacketHandler(buff);

        try {
            var newBuffer = packet.Buffer;
            var isDecrypted = EncDec.Decrypt(ref newBuffer, newBuffer.Length);
            Console.WriteLine($"IsDecrypted: {isDecrypted}");
            packet.Replace(newBuffer);
            packet.Pos = 0;

            packet.ReadInt32();
            var sender = packet.ReadUInt16();
            var opcode = packet.ReadUInt16();
            packet.ReadInt32();

            if(opcode != 0x30bf && opcode != 0x3005 && opcode != 0x3006)
                Console.WriteLine("Received Opcode: (0x{0:x2})", opcode);
        }
        catch(Exception) {
            Console.WriteLine("Failed to decrypt packet.");
        }
    }

    //private bool AuthenticateUser(string username, string password) {
    //    return username == "player" && password == "password";
    //}

    //private void SendLoginSuccess(Session session) {
    //    var packet = new PacketStream();
    //    packet.Write((ushort)0x0002);
    //    session.SendPacket(packet.GetBytes());
    //}

    //private void SendLoginFailure(Session session) {
    //    var packet = new PacketStream();
    //    packet.Write((ushort)0x0003);
    //    session.SendPacket(packet.GetBytes());
    //}
}
