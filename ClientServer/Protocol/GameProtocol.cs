using Shared.Network;
using Shared.Network.Encryption;
using Shared.Network.Stream;
using Shared.Protocol;

namespace ClientServer.Protocol;

public class GameProtocol : BaseProtocol {
    public override void OnConnect(Session session) {
        Console.WriteLine("Connected to the server!");
    }

    public override void OnReceive(Session session, byte[] buff, int bytes) {
        if(buff == null || buff.Length < 4) {
            Console.WriteLine("Invalid packet received.");
            return;
        }

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

            packet.ReadBytes(6);
            var opcode = packet.ReadUInt16();
            packet.ReadInt32();

            Console.WriteLine($"Opcode Client: {opcode}");
        }
        catch(Exception) {
            Console.WriteLine("Failed to decrypt packet.");
        }

        //if(opcode == 0x0002) {
        //    Console.WriteLine("Login successful!");
        //}
        //else if(opcode == 0x0003) {
        //    Console.WriteLine("Login failed.");
        //    session.Close();
        //}
        //else {
        //    Console.WriteLine($"Opcode Desconhecido {opcode}");
        //}
    }

    public override void OnDisconnect(Session session) {
        Console.WriteLine("Disconnected from the server.");
    }
}
