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
            Console.WriteLine($"Pacote recebido: {BitConverter.ToString(buff)}");
            packet.Replace(newBuffer);
            packet.Pos = 0;

            packet.ReadInt32();
            var sender = packet.ReadUInt16();
            var opcode = packet.ReadUInt16();
            packet.ReadInt32();

            Console.WriteLine($"Sender: 0x{sender:X}, Opcode: 0x{opcode:X}");

            switch(opcode) {
                case 0x81:
                HandleLogin(session, packet);
                break;
                case 0x685:
                Console.WriteLine("Chegou no OPCODE: 685");
                break;
                default:
                Console.WriteLine($"Opcode desconhecido: {opcode}");
                break;
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

        PacketHandler response = new PacketHandler();

        Console.WriteLine("Login bem-sucedido!");

        response.Write((ushort)0);  // Size (preenchido depois)
        response.Write((byte)0x00); // Key (pode precisar de ajuste)
        response.Write((byte)0x00); // ChkSum (se necessário, calcule depois)
        response.Write((ushort)12345); // Index (ID fictício do jogador)
        response.Write((ushort)0x82);  // Opcode de resposta
        response.Write((uint)Environment.TickCount); // Timestamp do login

        // Criando o Corpo do Pacote (TResponseLoginPacket)
        response.Write((uint)12345); // ID fictício do jogador
        response.Write((uint)Environment.TickCount); // Timestamp do login
        response.Write((ushort)1);  // Nação (exemplo)
        response.Write((uint)0);    // Null_1 (padding)

        // Preenchendo o tamanho correto do pacote no Header
        ushort packetSize = (ushort)response.Count;
        response.Buffer[0] = (byte)(packetSize & 0xFF);
        response.Buffer[1] = (byte)((packetSize >> 8) & 0xFF);

        // Enviando o pacote com Header
        byte[] packetData = response.GetBytes();
        Console.WriteLine($"HandleLogin enviado: {BitConverter.ToString(packetData)}");

        session.SendPacket(packetData);
    }

    private static bool VerificarCredenciais() {
        return true;
    }
}
