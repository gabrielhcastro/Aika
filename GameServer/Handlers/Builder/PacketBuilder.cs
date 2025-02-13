using GameServer.Handlers.Buffer;

namespace GameServer.Handlers.Builder;

public class PacketBuilder {
    private PacketHandler _packet;

    public PacketBuilder(ushort opcode, ushort index = 0) {
        _packet = new PacketHandler();

        _packet.Write((ushort)0);
        _packet.Write((byte)0x00);
        _packet.Write((byte)0x00);
        _packet.Write(index);
        _packet.Write(opcode);
        _packet.Write((uint)Environment.TickCount);
    }

    public PacketBuilder Write(byte value) { _packet.Write(value); return this; }
    public PacketBuilder Write(ushort value) { _packet.Write(value); return this; }
    public PacketBuilder Write(uint value) { _packet.Write(value); return this; }
    public PacketBuilder Write(long value) { _packet.Write(value); return this; }
    public PacketBuilder Write(bool value) { _packet.Write(value ? (byte)1 : (byte)0); return this; }
    public PacketBuilder Write(string value, int length) {
        _packet.Write(value, length);
        return this;
    }
    public PacketBuilder WriteBytes(byte[] value) { _packet.Write(value); return this; }

    public byte[] Build() {
        ushort packetSize = (ushort)_packet.Count;
        _packet.Buffer[0] = (byte)(packetSize & 0xFF);
        _packet.Buffer[1] = (byte)(packetSize >> 8 & 0xFF);
        return _packet.GetBytes();
    }
}

