using Shared.Handlers.Interface;
using Shared.Network.packet;
using Shared.Network.Packets;

namespace Shared.Handlers.Factory;

public static class PacketFactory {
    public static IPacketHandler CreatePacket(ushort opcode, PacketHandler packet) {
        return opcode switch {
            0x0081 => new LoginPacket(packet),
            0x0082 => new LoginResponsePacket(packet),
            _ => throw new ArgumentException($"Opcode desconhecido: {opcode:X2}")
        };
    }
}
