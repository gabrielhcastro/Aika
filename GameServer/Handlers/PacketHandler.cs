using GameServer.Core.Base;
using GameServer.Handlers.Builder;
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
            default:
            Console.WriteLine($"Unknown opcode: {opcode}, Sender: {sender}");
            break;
        }
    }

    private static async Task HandleLogin(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');
        string token = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');

        var account = await DatabaseHandler.Instance.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Login falhou: Conta não encontrada.");
            return;
        }

        account.Characters = await DatabaseHandler.Instance.GetCharactersByAccountIdAsync(account.Id);

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

        var account = await DatabaseHandler.Instance.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Login falhou: Conta não encontrada.");
            return;
        }

        account.Characters = await DatabaseHandler.Instance.GetCharactersByAccountIdAsync(account.Id);

        var packet = PacketBuilder.CreateHeader(0x901);

        packet.Write((uint)account.Id); // AccountID (fictício)
        packet.Write((uint)0); // Campo desconhecido (Unk)
        packet.Write((uint)0); // Campo não utilizado (NotUse)

        for(int i = 0; i < 3; i++) {
            var character = i < account.Characters.Count ? account.Characters.ToList()[i] : null;

            packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0') ?? new string('\0', 16))); // Nome ou vazio
            packet.Write((ushort)(account?.Nation ?? 0)); // Nação

            packet.Write(new byte[16]); // Equip zerado (por enquanto)
            packet.Write((byte)(character?.Slot ?? 0)); // Slot
            packet.Write((byte)(character?.Deleted ?? 0)); // Deletado?

            packet.Write((ushort)(character?.Strength ?? 0)); // Str
            packet.Write((ushort)(character?.Agility ?? 0)); // Agi
            packet.Write((ushort)(character?.Intelligence ?? 0)); // Int
            packet.Write((ushort)(character?.Constitution ?? 0)); // Cons
            packet.Write((ushort)(character?.Luck ?? 0)); // Luck
            packet.Write((ushort)(character?.Status ?? 0)); // Status

            packet.Write((byte)(character != null && !string.IsNullOrEmpty(character.NumericToken) ? 1 : 0)); // NumRegister
            packet.Write((byte)(character?.NumericErrors ?? 0)); // NumError

            packet.Write(new byte[] { 0x07, 0x77, 0x77, 0x00 }); // Altura padrão

            packet.Write((uint)(character?.Gold ?? 0)); // Gold
            packet.Write((uint)(character?.Experience ?? 0)); // Exp
            packet.Write((ushort)(character?.ClassInfo ?? 0)); // Classe
            packet.Write((ushort)(character?.Level ?? 0)); // Level
            packet.Write((ushort)(character?.PositionX ?? 0)); // Position X
            packet.Write((ushort)(character?.PositionY ?? 0)); // Position Y
            packet.Write((uint)0); // Sem tempo de exclusão

            if(!string.IsNullOrEmpty(character?.Name))
                Console.WriteLine($"Character: {character.Name} -> Carregado");
        }

        PacketBuilder.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        Console.WriteLine($"OK -> ChararactersList");
    }
}
