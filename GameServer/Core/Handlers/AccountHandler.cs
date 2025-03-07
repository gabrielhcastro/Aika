using GameServer.Core.Base;
using GameServer.Data.Repositories;
using GameServer.Network;
using System.Text;

namespace GameServer.Core.Handlers;
public class AccountHandler{
    public static async Task AccountLogin(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');
        string password = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');
        Console.WriteLine($"Username: {username}");
        Console.WriteLine($"Password: {password}");

        var account = await AccountRepository.GetAccountByUsernameAsync(username);
        if(account == null) {
            CharacterHandler.GameMessage(session, 16, 0, "CONTA NAO ENCONTRADA!");
            return;
        }

        var packet = PacketFactory.CreateHeader(0x82);

        packet.Write(account.Id); // AccountID
        packet.Write((uint)Environment.TickCount); // LoginTime
        packet.Write((uint)account.Nation); // Nação
        packet.Write((byte)0); // Null_1 (padding)

        packet.Buffer[0] = 0x19; // size 25 bytes fixo login porta 8831
        packet.Buffer[1] = 0x00;

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);
        session.LastActivity = DateTime.Now;

        PacketPool.Return(packet);
    }
}
