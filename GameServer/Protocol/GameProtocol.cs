using Shared.Base;
using Shared.Handlers.Builder;
using Shared.Network;
using Shared.Network.Encryption;
using Shared.Network.packet;
using System.Text;

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
            Console.WriteLine($"Packet Decriptado: {isDecrypted}");
            packet.Replace(newBuffer);
            packet.Pos = 0;

            packet.ReadInt32();
            var sender = packet.ReadUInt16();
            var opcode = packet.ReadUInt16();
            packet.ReadInt32();

            Console.WriteLine($"Sender Recebido: (0x{sender:X})");
            Console.WriteLine($"Opcode Recebido: (0x{opcode:X})");
            Console.WriteLine($"Pacote Recebido: {BitConverter.ToString(buff)}");

            if(opcode == 0x81) {
                HandleLogin(session, packet);
            }

        }
        catch(Exception) {
            Console.WriteLine("Failed to decrypt packet.");
        }

        //if(opcode == 0x0001) {
        //    var response = new PacketHandler();
        //    response.Write((ushort)0x0002);
        //    session.SendPacket(response.GetBytes());
        //}
        //else if(opcode == 0x0002) {
        //    Console.WriteLine($"Pong recebido de {session.Ip}");
        //    session.LastPongTime = DateTime.UtcNow;
        //}
    }

    private void HandleLogin(Session session, PacketHandler packet) {
        string username = Encoding.ASCII.GetString(packet.ReadBytes(32)).TrimEnd('\0');
        string token = Encoding.ASCII.GetString(packet.ReadBytes(32)).TrimEnd('\0');

        Console.WriteLine($"Usuário tentando login: {username}, Token: {token}");

        bool loginValido = VerificarCredenciais();

        if(loginValido) {
            Console.WriteLine("Login bem-sucedido!");

            PacketHandler response = new PacketHandler();

            var loginPacket = new PacketBuilder(0x685)
                                    .Write((uint)123456) // AccountId
                                    .Write("admin", 32) // Username
                                    .Write((uint)Environment.TickCount) // Timestamp
                                    .WriteBytes(new byte[14]) // MacAddr
                                    .Write((ushort)304) // Version
                                    .Write((uint)0) // Null
                                    .Write(token, 32) // Token
                                    .WriteBytes(new byte[991]) // Null_1 (padding)
                                    .Build();

            session.SendPacket(loginPacket);

            byte[] packetData = response.GetBytes();
            Console.WriteLine($"Pacote de resposta: {BitConverter.ToString(packetData)}");

            session.SendPacket(packetData);
        }
        else {
            Console.WriteLine("Login falhou.");
            session.Close();
        }
    }

    private static bool VerificarCredenciais() {
        return true;
    }
}
