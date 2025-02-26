using GameServer.Core.Base;
using GameServer.Data;
using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Model.Item;
using GameServer.Network;
using System.Text;
using System.Text.RegularExpressions;

namespace GameServer.Core.Handlers;
public static class PacketHandler {
    public static async Task HandlePacket(Session session, StreamHandler stream) {
        stream.ReadInt32();
        var sender = stream.ReadUInt16();
        var opcode = stream.ReadUInt16();
        stream.ReadInt32();

        Console.WriteLine($"Opcode received: {opcode}, Sender: {sender}");

        switch(opcode) {
            case 0x81:
            await AccountHandler.AccountLogin(session, stream);
            break;
            case 0x685:
            await CharacterHandler.SelectedNation(session, stream);
            break;
            case 0x3E04:
            await CharacterHandler.CreateCharacter(session, stream);
            break;
            case 0x39D:
            break;
            case 0xF02:
            await CharacterHandler.SendToWorld(session, stream);
            break;
            case 0xF0B:
            CharacterHandler.SendToWorldSends(session);
            break;
            case 0x668:
            await CharacterHandler.ChangeCharacter(session);
            break;
            case 0x305:
            CharacterHandler.UpdateRotation(stream, session);
            break;
            default:
            Console.WriteLine($"Unknown opcode: {opcode}, Sender: {sender}");
            Console.WriteLine("Packet Data: {0} -> {1}", stream.Count, BitConverter.ToString(stream));
            break;
        }
    }
}