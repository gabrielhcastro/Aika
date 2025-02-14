using GameServer.Core.Base;
using GameServer.Handlers.Builder;

namespace GameServer.Handlers;
public static class PacketHandler {
    public static void HandlePacket(Session session, ushort opcode) {
        switch(opcode) {
            case 0xF311:
            Console.WriteLine("Handshake recebido, aguardando login...");
            return;
            case 0x81:
            HandleLogin(session);
            break;
            case 0x685:
            SendToCharactersList(session);
            break;
            default:
            Console.WriteLine($"Unknown opcode: {opcode}");
            break;
        }
    }

    private static void HandleLogin(Session session) {
        var packet = PacketBuilder.CreateHeader(0x82);

        packet.Write((uint)12345); // ID fictício do jogador
        packet.Write((uint)11981171); 
        packet.Write((ushort)0); // Nação (exemplo)
        packet.Write((uint)0); // Null_1 (padding)

        packet.Buffer[0] = (byte)(0x19); // size 25 bytes fixo login porta 8831
        packet.Buffer[1] = (byte)(0x00);

        byte[] packetData = packet.GetBytes();
        session.SendPacket(packetData);

        Console.WriteLine($"OK -> HandleLogin");
    }

    private static void SendToCharactersList(Session session) {
        var packet = PacketBuilder.CreateHeader(0x901);

        packet.Write((uint)1); // AccountID (fictício)
        packet.Write((uint)0); // Campo desconhecido (Unk)
        packet.Write((uint)0); // Campo não utilizado (NotUse)

        // Dados dos personagens (3 personagens)
        packet.Write(new byte[16]); // Nome vazio (16 bytes)
        packet.Write((ushort)0); // Nação
        packet.Write(new byte[16]); // Equipamentos zerados
        packet.Write((byte)0); // Refinamento

        // **Atributos zerados**
        packet.Write(new byte[sizeof(ushort) * 6]); // Str, Agi, Int, Cons, Luck, Status

        packet.Write((byte)0); // Numeric Token
        packet.Write((byte)0); // Numeric Error

        // **Tamanho padrão (07 77 77)**
        packet.Write(new byte[] { 0x07, 0x77, 0x77, 0x00 });

        packet.Write((uint)0); // Ouro
        packet.Write((uint)0); // Exp
        packet.Write((ushort)0); // Classe
        packet.Write((ushort)0); // Nível
        packet.Write((ushort)0); // Equip extra
        packet.Write((ushort)0);
        packet.Write((uint)0); // Sem tempo de exclusão

        PacketBuilder.FinalizePacket(packet); // packet size ajustado

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        Console.WriteLine($"OK -> ChararactersList");
    }
}
