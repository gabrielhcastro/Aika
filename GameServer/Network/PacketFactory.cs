using GameServer.Core.Handlers;

namespace GameServer.Network;
public static class PacketFactory {
    public static StreamHandler CreateHeader(ushort opcode, ushort index = 0) {
        var packet = PacketPool.Rent();

        var header = new PacketHeader(opcode, index);

        Span<byte> headerData = stackalloc byte[8]; // Stack (zero GC)
        BitConverter.TryWriteBytes(headerData.Slice(0, 2), (ushort)0);
        headerData[2] = header.Key;
        headerData[3] = header.ChkSum;
        BitConverter.TryWriteBytes(headerData.Slice(4, 2), header.Index);
        BitConverter.TryWriteBytes(headerData.Slice(6, 2), header.Code);

        packet.Write(headerData);
        packet.Write(header.Time);

        return packet;
    }

    public static void FinalizePacket(StreamHandler stream) {
        ushort packetSize = (ushort)stream.Count;
        stream.Buffer[0] = (byte)(packetSize & 0xFF);
        stream.Buffer[1] = (byte)(packetSize >> 8);
    }
}