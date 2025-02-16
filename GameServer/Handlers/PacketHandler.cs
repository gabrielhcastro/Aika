using GameServer.Core.Base;
using GameServer.Handlers.Builder;
using Shared.Handlers;
using Shared.Models.Character;
using System.Runtime.InteropServices;
using System.Text;

namespace GameServer.Handlers;
public static class PacketHandler {
    public static async Task HandlePacket(Session session, StreamHandler packet) {
        packet.ReadInt32();
        var sender = packet.ReadUInt16();
        var opcode = packet.ReadUInt16();
        packet.ReadInt32();

        switch(opcode) {
            case 0x81:
            await HandleLogin(session, packet);
            break;
            case 0x685:
            await SendToCharactersList(session, packet);
            break;
            //case 0x3E04:
            //await CreateCharacter(session, packet);
            //break;
            default:
            Console.WriteLine($"Unknown opcode: {opcode}, Sender: {sender}");
            break;
        }
    }

    private static async Task HandleLogin(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');
        string token = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');

        var account = await DatabaseHandler.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Login falhou: Conta não encontrada.");
            return;
        }

        var packet = PacketBuilder.CreateHeader(0x82);

        packet.Write((uint)account.Id); // ID fictício do jogador
        packet.Write((uint)Environment.TickCount);
        packet.Write((ushort)account.Nation); // Nação (exemplo)
        packet.Write((uint)0); // Null_1 (padding)

        packet.Buffer[0] = (byte)(0x19); // size 25 bytes fixo login porta 8831
        packet.Buffer[1] = (byte)(0x00);

        byte[] packetData = packet.GetBytes();
        session.SendPacket(packetData);

        Console.WriteLine($"Login -> {username} : {token}");
    }

    private static async Task SendToCharactersList(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');
        _ = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0'); //TOKEN

        var account = await DatabaseHandler.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Login falhou: Conta não encontrada.");
            return;
        }

        account.Characters = await DatabaseHandler.GetCharactersByAccountIdAsync(account.Id);

        var packet = PacketBuilder.CreateHeader(0x901, (ushort)account.Id);

        packet.Write((uint)account.Id); // AccountID (fictício)
        packet.Write((uint)0); // Campo desconhecido (Unk)
        packet.Write((uint)0); // Campo não utilizado (NotUse)

        for(int i = 0; i < 3; i++) {
            var character = i < account.Characters.Count ? account.Characters.ToList()[i] : null;

            // Nome
            packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0') ?? new string('\0', 16))); 

            packet.Write((ushort)(account?.Nation ?? 0)); // Nação
            packet.Write((ushort)(character?.ClassInfo ?? 0)); // Classe

            packet.Write((byte)7); // Altura
            packet.Write((byte)119); // Tronco
            packet.Write((byte)119); // Perna
            packet.Write((byte)0); // Corpo

            for(int k = 0; k < 8; k++) {
                packet.Write((ushort)0); //Equipamento
            }

            for(int k = 0; k < 12; k++) {
                packet.Write((byte)0); //Refine?
            }

            packet.Write((ushort)(character?.Strength ?? 0)); // Str
            packet.Write((ushort)(character?.Agility ?? 0)); // Agi
            packet.Write((ushort)(character?.Intelligence ?? 0)); // Int
            packet.Write((ushort)(character?.Constitution ?? 0)); // Cons
            packet.Write((ushort)(character?.Luck ?? 0)); // Luck
            packet.Write((ushort)(character?.Status ?? 0)); // Status

            packet.Write((ushort)(character?.Level ?? 65535)); // Level

            packet.Write(new byte[6]); // Null

            packet.Write((long)(character?.Experience ?? 0)); // Exp
            packet.Write((long)(character?.Gold ?? 0)); // Gold

            packet.Write(new byte[4]); // Null

            packet.Write((uint)0); // Sem tempo de exclusão
            packet.Write((byte)(character?.NumericErrors ?? 0)); // NumError
            packet.Write((bool)(true)); // NumRegister

            packet.Write(new byte[6]); // Null

            if(!string.IsNullOrEmpty(character?.Name))
                Console.WriteLine($"Character: {character.Name} -> Carregado");
        }

        Console.WriteLine("Packet Data Decrypted: {0}",BitConverter.ToString(packet));
        PacketBuilder.FinalizePacket(packet);
        Console.WriteLine("Packet Data Encrypted: {0}",BitConverter.ToString(packet));

        //Raw Packet

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        Console.WriteLine($"OK -> ChararactersList");
    }

    //private static async Task CreateCharacter(Session session, StreamHandler stream) {
    //    uint accountId = stream.ReadUInt32(); // AccountId
    //    uint slotId = stream.ReadUInt32(); // SlotId
    //    string name = Encoding.ASCII.GetString(stream.ReadBytes(16)).PadRight(16, '\0').TrimEnd(); // Name
    //    ushort classInfo = stream.ReadUInt16(); // ClassInfo
    //    ushort hair = stream.ReadUInt16();
    //    byte[] null1 = stream.ReadBytes(12);
    //    uint local = stream.ReadUInt32();

    //}
}
