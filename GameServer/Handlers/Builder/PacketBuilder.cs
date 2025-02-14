using GameServer.Network.Packets;

namespace GameServer.Handlers.Builder;

public static class PacketBuilder {
    public static StreamHandler CreateHeader(ushort opcode, ushort index = 0) {
        var packet = new StreamHandler();
        var header = new PacketHeader(opcode, index);

        packet.Write((ushort)0);
        packet.Write(header.Key);
        packet.Write(header.ChkSum);
        packet.Write(header.Index);
        packet.Write(header.Code);
        packet.Write((uint)11981171);

        return packet;
    }

    public static void FinalizePacket(StreamHandler packet) {
        ushort packetSize = (ushort)packet.Count;
        packet.Buffer[0] = (byte)(packetSize & 0xFF);
        packet.Buffer[1] = (byte)(packetSize >> 8);
    }
}

