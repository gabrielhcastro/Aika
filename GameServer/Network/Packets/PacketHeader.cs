namespace GameServer.Network.Packets;

public struct PacketHeader {
    public ushort Size;
    public byte Key;
    public byte ChkSum;
    public ushort Index;
    public ushort Code;
    public uint Time;
}
