namespace GameServer.Network.Packets;
public class RequestLoginPacket {
    public PacketHeader Header;
    public uint AccountId;
    public string Username;
    public uint Time;
    public byte[] MacAddr = new byte[14];
    public ushort Version;
    public uint Null;
    public string Token;
    public byte[] Null_1 = new byte[992];
}
