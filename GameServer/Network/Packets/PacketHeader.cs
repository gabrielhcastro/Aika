namespace GameServer.Network.Packets;

public struct PacketHeader {
    public byte Key;
    public byte ChkSum;
    public ushort Index;
    public ushort Code;
    public uint Time;

    public PacketHeader(ushort opcode, ushort index = 0x0000) {
        Key = 0x00;
        ChkSum = 0x00;
        Index = index;
        Code = opcode;
        Time = (uint)(new TimeSpan(DateTime.Now.Ticks - (new DateTime(2001, 1, 1)).Ticks)).TotalSeconds;
    }
}
