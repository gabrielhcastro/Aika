using GameServer.Core.Base;
using GameServer.Data;
using GameServer.Data.Repositories;
using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Network;
using GameServer.Service;
using MySqlConnector;
using Shared.Core;
using System.Text;

namespace GameServer.Core.Handlers;
public class AccountHandler{
    // VERIFICAR SE TA BANIDO
    // VERIFICAR DIAS DE BAN CASO BANIDO
    // VERIFICAR GOLD DO BAU
    // VERIFICAR CASH DA CONTA
    // VERIFICAR TEMPO DE EXPIRAÇÃO DO PREMIUM
    // VERIFICAR NAÇÃO
    // VERIFICAR O STATUS/TIPO DA CONTA

    public static async Task AccountLogin(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');
        string password = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');
        Console.WriteLine($"Password: {password}");

        var account = await AccountRepository.GetAccountByUsernameAsync(username);
        if(account == null) {
            CharacterHandler.GameMessage(session, 16, 0, "CONTA NÃO ENCONTRADA!");
            return;
        }

        var packet = PacketFactory.CreateHeader(0x82);

        packet.Write((uint)account.Id); // AccountID
        packet.Write((uint)Environment.TickCount); // LoginTime
        packet.Write((ushort)account.Nation); // Nação
        packet.Write((uint)0); // Null_1 (padding)

        packet.Buffer[0] = 0x19; // size 25 bytes fixo login porta 8831
        packet.Buffer[1] = 0x00;

        byte[] packetData = packet.GetBytes();
        session.SendPacket(packetData);
        session.LastActivity = DateTime.Now;

        PacketPool.Return(packet);
    }
}
