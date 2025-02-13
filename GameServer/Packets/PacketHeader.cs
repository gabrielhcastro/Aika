namespace GameServer.Packets;

public class PacketHeader {
    public ushort Size;      // Tamanho total do pacote
    public byte Key;          // Chave de criptografia
    public byte ChkSum;       // Checksum (opcional, depende da lógica)
    public ushort Index;      // Índice do jogador ou sessão
    public ushort Code;       // Opcode (ex.: 0x685)
    public uint Time;         // Timestamp
}
