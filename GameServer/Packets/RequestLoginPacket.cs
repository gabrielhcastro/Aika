namespace GameServer.Packets;
public class RequestLoginPacket {
    public PacketHeader Header;
    public uint AccountId;                   // ID da conta
    public string Username;                 // Nome de usuário (32 bytes, preenchido com '\0')
    public uint Time;                        // Timestamp
    public byte[] MacAddr = new byte[14];   // Endereço MAC (14 bytes)
    public ushort Version;                  // Versão do cliente
    public uint Null;                       // Campo nulo (4 bytes)
    public string Token;                    // Token (32 bytes, preenchido com '\0')
    public byte[] Null_1 = new byte[992];   // Preenchimento (992 bytes)
}
