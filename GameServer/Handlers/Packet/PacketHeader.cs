namespace GameServer.Handlers.Packet;

public struct PacketHeader(ushort opcode, ushort index = 0x0000) {
    public byte Key = 0x00;
    public byte ChkSum = 0x00;
    public ushort Index = index;
    public ushort Code = opcode;
    public uint Time = (uint)new TimeSpan(DateTime.Now.Ticks - new DateTime(2001, 1, 1).Ticks).TotalSeconds;
}
