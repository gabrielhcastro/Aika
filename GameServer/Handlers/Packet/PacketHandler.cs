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

        packet.Write((uint)account.Id); // AccountID (fictício)
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

            packet.Write((uint)character.Deleted); // Deleted?
            packet.Write((byte)(character?.NumericErrors ?? 0)); // Numeric Erros

            if(!string.IsNullOrEmpty(character.NumericToken)) {
                packet.Write(true); // Numeric Registered?
            } else {
                packet.Write(false); // Numeric Registered?
            }

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

        // Informações básicas do personagem
        packet.Write((uint)1); // CharIndex
        packet.Write((byte)0);
        packet.Write((byte)17); // Algum ID (17 = 0x11)
        packet.Write(Encoding.ASCII.GetBytes("Vitor".PadRight(16, '\0'))); // Nome
        packet.Write((ushort)0);
        packet.Write((byte)31); // Algum valor (0x1F)
        packet.Write((ushort)0);

        // Status base (igual ao Delphi)
        packet.Write((byte)8);
        packet.Write((byte)14);
        packet.Write((byte)10);
        packet.Write((byte)12);
        packet.Write((byte)6);
        packet.Write((byte)0);

        // Altura e corpo
        packet.Write((byte)7);
        packet.Write((byte)119);
        packet.Write((byte)119);
        packet.Write((byte)0);

        // HP, Mana
        packet.Write((uint)468); // Max HP
        packet.Write((uint)468); // Current HP
        packet.Write((uint)342); // Max Mana
        packet.Write((uint)342); // Current Mana

        // Tempo de login
        packet.Write((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // LoginTime
        packet.Write((uint)0);
        packet.Write((uint)0);
        packet.Write((uint)0);

        // Outros atributos
        packet.Write((ushort)3);
        packet.Write((ushort)0);

        // Inventário (igual ao Delphi, preenchido com 0)
        for(int i = 0; i < 60; i++)
            packet.Write((ushort)0);

        // Buffs
        for(int i = 0; i < 20; i++)
            packet.Write((ushort)0);

        for(int i = 0; i < 20; i++)
            packet.Write((uint)0);

        // Sistema de Títulos
        packet.Write((ushort)0);
        for(int i = 0; i < 12; i++)
            packet.Write((uint)0);

        for(int i = 0; i < 48; i++)
            packet.Write((ushort)0);

        packet.Write((ushort)1985); // GuildIndex?
        packet.Write((ushort)4);
        packet.Write((ushort)0);

        // Finalizando o pacote
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
