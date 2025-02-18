using GameServer.Core.Base;
using Shared.Handlers;
using System.IO;
using System.Text;

namespace GameServer.Handlers.Packet;
public static class PacketHandler {
    public static async Task HandlePacket(Session session, StreamHandler packet) {
        packet.ReadInt32();
        var sender = packet.ReadUInt16();
        var opcode = packet.ReadUInt16();
        packet.ReadInt32();

        Console.WriteLine($"Opcode received: {opcode}, Sender: {sender}");

        switch(opcode) {
            case 0x81:
            await HandleLogin(session, packet);
            break;
            case 0x685:
            await SendToCharactersList(session, packet);
            break;
            case 0x39D:
            Console.WriteLine("OPCODE: 925");
            Console.WriteLine("Packet Data: {0} -> {1}", packet.Count, BitConverter.ToString(packet));
            break;
            case 0xF02:
            HandleCharacterNumeric(session, packet);
            break;
            default:
            Console.WriteLine($"Unknown opcode: {opcode}, Sender: {sender}");
            Console.WriteLine("Packet Data: {0} -> {1}", packet.Count, BitConverter.ToString(packet));
            break;
        }
    }

    private static async Task HandleLogin(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');

        var account = await DatabaseHandler.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Login falhou: Conta não encontrada.");
            return;
        }

        var packet = PacketFactory.CreateHeader(0x82);

        packet.Write((uint)account.Id); // AccountID
        packet.Write((uint)session.LastActivity.Ticks); // LoginTime
        packet.Write((ushort)account.Nation); // Nação
        packet.Write((uint)0); // Null_1 (padding)

        packet.Buffer[0] = 0x19; // size 25 bytes fixo login porta 8831
        packet.Buffer[1] = 0x00;

        byte[] packetData = packet.GetBytes();
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine($"Login -> {username} : {DateTime.UtcNow}");
    }

    private static async Task SendToCharactersList(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');
        _ = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');

        var account = await DatabaseHandler.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Login falhou: Conta não encontrada.");
            return;
        }

        account.Characters = await DatabaseHandler.GetCharactersByAccountIdAsync(account.Id);

        var packet = PacketFactory.CreateHeader(0x901);

        packet.Write((uint)account.Id); // AccountID
        packet.Write((uint)0); // Campo desconhecido (Unk)
        packet.Write((uint)0); // Campo não utilizado (NotUse)

        for(int i = 0; i < 3; i++) {
            var character = i < account.Characters.Count ? account.Characters.ToList()[i] : null;

            // Nome
            packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0') ?? new string('\0', 16)));

            packet.Write((ushort)(account?.Nation ?? 0)); // Nação
            packet.Write((ushort)(character?.ClassInfo ?? 0)); // Classe

            if(character == null) {
                packet.Write((byte)7); // Altura
                packet.Write((byte)119); // Tronco
                packet.Write((byte)119); // Perna
                packet.Write((byte)0); // Corpo
            }
            else {
                packet.Write((byte)character.Height); // Altura
                packet.Write((byte)character.Trunk); // Tronco
                packet.Write((byte)character.Leg); // Perna
                packet.Write((byte)character.Body); // Corpo
            }

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

            packet.Write((ushort)(character?.Level ?? 65534)); // Level

            packet.Write(new byte[6]); // Null

            packet.Write((long)(character?.Experience ?? 0)); // Exp
            packet.Write((long)(character?.Gold ?? 0)); // Gold

            packet.Write(new byte[4]); // Null

            if(character != null) {
                packet.Write((uint)character.Deleted); // Deleted?
            }
            else {
                packet.Write((uint)0);
            }

            packet.Write((byte)(character?.NumericErrors ?? 0)); // Numeric Erros

            packet.Write(true); // Numeric Registered?

            packet.Write(new byte[6]); // NotUse

            if(!string.IsNullOrEmpty(character?.Name))
                Console.WriteLine($"Character: {character.Name} -> Carregado");
        }

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine($"OK -> ChararactersList");
    }

    private static void HandleCharacterNumeric(Session session, StreamHandler stream) {
        var packet = PacketFactory.CreateHeader(0x925, 0x7535);

        packet.Write((uint)1); // AccountSerial

        packet.Write((uint)1); // AccountId
        packet.Write((byte)0); // First Login
        packet.Write((byte)1); // CharacterId

        packet.Write(Encoding.ASCII.GetBytes("Vitor".PadRight(16, '\0'))); // Name

        packet.Write((ushort)0); // Nation
        packet.Write((byte)10); // Classe
        packet.Write((ushort)0); // Null_0

        packet.Write((ushort)8); // Strength
        packet.Write((ushort)14); // Agility
        packet.Write((ushort)10); // Intelligence
        packet.Write((ushort)12); // Constitution
        packet.Write((ushort)6); // Luck
        packet.Write((ushort)0); // Status

        packet.Write((byte)7); // Height
        packet.Write((byte)119); // Trunk
        packet.Write((byte)119); // Leg
        packet.Write((byte)0); // Body

        packet.Write((uint)468); // Max HP
        packet.Write((uint)468); // Current HP
        packet.Write((uint)342); // Max Mana
        packet.Write((uint)342); // Current Mana

        packet.Write((uint)1739944800); // ServerReset
        packet.Write((uint)0); // Honor
        packet.Write((uint)0); // KillPoint
        packet.Write((uint)0); // Infamia
        packet.Write((ushort)0); // Evil Points
        packet.Write((ushort)0); // Skill Points

        packet.Write(new byte[60]); // Inventory

        packet.Write((ushort)0); // Unk1

        packet.Write((ushort)0); // Physic Damage
        packet.Write((ushort)0); // Physic Defense
        packet.Write((ushort)0); // Magic Damage
        packet.Write((ushort)0); // Magic Defense
        packet.Write((ushort)0); // Bonus Damage

        packet.Write(new byte[20]); // Null_1

        packet.Write((ushort)0); // Critical
        packet.Write((byte)0); // Evasion
        packet.Write((byte)0); // Accuracy

        packet.Write((ushort)0);     // Null_2

        packet.Write((uint)1985);    // Exp
        packet.Write((ushort)4);     // Level
        packet.Write((ushort)0);     // GuildIndex

        packet.Write(new byte[64]);  // Null_3

        for(int i = 0; i < 20; i++)
            packet.Write((ushort)0); // BuffsId

        for(int i = 0; i < 20; i++)
            packet.Write((uint)0);   // BuffsDuration

        // Equipamentos (16 slots)
        for(int i = 0; i < 16; i++) {
            packet.Write((ushort)0);
            packet.Write((ushort)0);
            packet.Write((ushort)0);
            packet.Write((ushort)0);
        }

        packet.Write((ushort)0);  // Null

        // Inventário (64 slots)
        for(int i = 0; i < 64; i++) {
            packet.Write((ushort)0);
            packet.Write((ushort)0);
            packet.Write((ushort)0);
            packet.Write((ushort)0);
        }

        packet.Write((long)0);  // Gold

        packet.Write(new byte[256]); // UnkBytes0
        packet.Write(new byte[160]); // UnkBytes1
        packet.Write((uint)0); // CreationTime
        packet.Write(new byte[256]); // UnkBytes2

        var numeric = Encoding.ASCII.GetBytes("0000");
        packet.Write(numeric);

        packet.Write(new byte[256]); // UnkBytes3

        for(int i = 0; i < 60; i++)
            packet.Write((ushort)(i < 4 ? 2 : 0)); // Habilidades iniciais

        // Barra de itens (24 slots)
        ushort[] itemBar = { 46098, 47634, 46610, 0, 0, 0, 0, 46354 };
        for(int i = 0; i < 24; i++)
            packet.Write((ushort)(i < itemBar.Length ? itemBar[i] : 0));

        packet.Write((ushort)0); // NULL_5

        // Títulos e progresso de título
        for(int i = 0; i < 12; i++)
            packet.Write((uint)0); // TitleCategoryLevel

        packet.Write(new byte[96]); // UNK_8
        packet.Write((ushort)0);    // ActiveTitle
        packet.Write((ushort)0);    // NULL_9

        for(int i = 0; i < 48; i++)
            packet.Write((ushort)0); // TitleProgressType8

        packet.Write((ushort)0); // TitleProgressType9[0]
        packet.Write((ushort)0); // TitleProgressType9[1]
        packet.Write((ushort)0); // TitleProgressType4
        packet.Write((ushort)0); // TitleProgressType10
        packet.Write((ushort)0); // TitleProgressType7
        packet.Write((ushort)0); // TitleProgressType11
        packet.Write((ushort)0); // TitleProgressType12
        packet.Write((ushort)0); // TitleProgressType13
        packet.Write((ushort)0); // TitleProgressType15
        packet.Write((ushort)0); // TitleProgressUnk
        packet.Write(new byte[22]); // TitleProgressType16
        packet.Write((ushort)0); // TitleProgressType23

        packet.Write(new byte[200]); // TitleProgress
        packet.Write((uint)0); // EndDayTime

        // Zerando mais dados desconhecidos
        packet.Write(new byte[128]); // Null_10
        packet.Write((uint)0);       // UTC
        packet.Write((uint)0); // LoginTime
        packet.Write(new byte[12]);  // UnkBytes6

        // Nomes dos Prans (16 bytes cada)
        packet.Write(new byte[16]); // PranName[0]
        packet.Write(new byte[16]); // PranName[1]

        packet.Write((uint)0); // Unknow


        PacketFactory.FinalizePacket(packet);

        Console.WriteLine("Packet Decrypt: {0} -> {1}", packet.Count, BitConverter.ToString(packet));

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine("Packet Encrypt: {0} -> {1}", packet.Count, BitConverter.ToString(packetData));
        Console.WriteLine($"OK -> ChararactersList");
    }
}
