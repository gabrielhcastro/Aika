using GameServer.Core.Base;
using GameServer.Core.Handlers;
using GameServer.Network;

namespace GameServer.Core.Protocol;

public class GameProtocol : BaseProtocol {
    public override void OnConnect(Session session) {
        Console.WriteLine($"Player connected from {session.Ip}");
    }

    public override void OnDisconnect(Session session) {
        Console.WriteLine($"Player disconnected from {session.Ip}");
    }

    public override async void OnReceive(Session session, byte[] buff, int bytes) {
        if(buff == null || buff.Length < 4) {
            Console.WriteLine("Invalid packet received.");
            return;
        }

        if(buff.Length > 2 && buff[0] == 0x11 && buff[1] == 0xF3) {
            var newBuff = new byte[buff.Length - 4];
            Array.Copy(buff, 4, newBuff, 0, buff.Length - 4);
            buff = newBuff;
        }

        var packet = new StreamHandler(buff);

        try {
            if(packet.Count < 12) {
                Console.WriteLine("Packet too small.");
                return;
            }

            var size = packet.ReadUInt16();

            if(packet.Count >= size) {
                packet.Replace(packet.Buffer, 0, size);
                packet.Pos = 0;

                var isDecrypted = false;
                try {
                    var newBuffer = packet.Buffer;
                    isDecrypted = EncDec.Decrypt(ref newBuffer, newBuffer.Length);
                    packet.Replace(newBuffer);
                    packet.Pos = 0;
                }
                catch(Exception e) {
                    Console.WriteLine($"Failed to decrypt packet: {e.Message}");
                    return;
                }

                if(isDecrypted) {
                    await PacketHandler.HandlePacket(session, packet);
                }
                else {
                    Console.WriteLine("Failed to decrypt packet.");
                }
            }
            else {
                Console.WriteLine($"Packet with wrong size. ({packet.Count} >= {size})");
            }
        }
        catch(Exception e) {
            Console.WriteLine($"Error processing packet: {e.Message}");
            session.Close();
        }
    }
}
