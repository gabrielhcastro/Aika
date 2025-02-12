using Shared.Network;
using Shared.Network.Base;
using Shared.Network.Encryption;
using System.IO;
using System.Net;

namespace GameServer;

public class GameProtocol : BaseProtocol {
    public override void OnConnect(Session session) {
        Console.WriteLine($"Player connected from {session.Ip}");
    }

    public override void OnDisconnect(Session session) {
        Console.WriteLine($"Player disconnected from {session.Ip}");
    }

    public override void OnReceive(Session session, byte[] buff, int bytes) {
        if(buff.Length > 2 && buff[0] == 0x11 && buff[1] == 0xF3) {
            var newBuff = new byte[buff.Length - 4];
            Array.Copy(buff, 4, newBuff, 0, buff.Length - 4);
            buff = newBuff;
        }

        var packet = new PacketStream(buff);

        try {
            var newBuffer = packet.Buffer;
            var isDecrypted = Encryption.Decrypt(ref newBuffer, newBuffer.Length);
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

        //if(opcode == 0x0001) {
        //    var username = packet.ReadString(32).Trim('\0');
        //    var password = packet.ReadString(32).Trim('\0');

        //    if(AuthenticateUser(username, password)) {
        //        SendLoginSuccess(session);
        //        Console.WriteLine($"Player {username} authenticated successfully.");
        //    }
        //    else {
        //        SendLoginFailure(session);
        //        Console.WriteLine($"Failed login attempt for {username}.");
        //    }
        //}
        //else {
        //    Console.WriteLine($"Opcode Desconhecido {opcode}");
        //}
    }

    private bool AuthenticateUser(string username, string password) {
        return username == "player" && password == "password";
    }

    private void SendLoginSuccess(Session session) {
        var packet = new PacketStream();
        packet.Write((ushort)0x0002);
        session.SendPacket(packet.GetBytes());
    }

    private void SendLoginFailure(Session session) {
        var packet = new PacketStream();
        packet.Write((ushort)0x0003);
        session.SendPacket(packet.GetBytes());
    }
}

class Program {
    static void Main(string[] args) {
        var endpoint = new IPEndPoint(IPAddress.Any, 8831);
        var protocol = new GameProtocol();
        var server = new Server(endpoint, 100, protocol);

        Console.WriteLine("Starting server...");
        server.Start();

        Console.WriteLine("Press ENTER to stop the server.");
        Console.ReadLine();

        server.Stop();
        Console.WriteLine("Server stopped.");
    }
}
