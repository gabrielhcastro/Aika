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
            await CharacterHandler.SelectedChannel(session, stream);
            break;
            case 0x3E04:
            await CharacterHandler.CreateCharacter(session, stream);
            break;
            case 0x39D:
            break;
            case 0xF02:
            await CharacterHandler.SendToNation(session, stream);
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
    
    private static void CreateCharacterMob(Session session, byte spawnType) {
        if(session == null) return;

        var account = session.ActiveAccount;
        var character = session.ActiveCharacter;

        SpawnCharacter(session, account, character);

        var packet = PacketFactory.CreateHeader(0x349, (ushort)account.Id);

        packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0')));

        for(int i = 0; i < 8; i++) {
            packet.Write((ushort)0); // TO-DO: Equips
        }

        for(int i = 0; i < 12; i++) {
            packet.Write((byte)0); // TO-DO: ItemEffect
        }

        // Talvez seja assim!?
        packet.Write((float)character.PositionX);
        packet.Write((float)character.PositionY);

        packet.Write(character.Rotation);
        packet.Write(character.MaxHealth);
        packet.Write(character.MaxMana);
        packet.Write(character.CurrentHealth);
        packet.Write(character.CurrentMana);

        packet.Write((byte)0x0A); // Unk

        packet.Write((byte)character.SpeedMove);
        packet.Write(spawnType); // TO-DO: SpawnType
        packet.Write((byte)character.Height);
        packet.Write((byte)character.Trunk);
        packet.Write((byte)character.Leg);
        packet.Write((byte)character.Body);
        packet.Write((byte)0); // IsService (0 Player, 1 Npc?!)

        packet.Write((ushort)0); // TO-DO: EffectType
        packet.Write((ushort)0); // TO-DO: SetBuffs

        for(int i = 0; i < 60; i++) {
            packet.Write((ushort)0); // TO-DO: Buffs
        }

        for(int i = 0; i < 60; i++) {
            packet.Write((uint)0); // TO-DO: BuffTime
        }

        packet.Write(Encoding.ASCII.GetBytes(new string('\0', 32))); // BuffTime

        packet.Write((ushort)account.Nation * 4096); // TO-DO: Guild Index and Nation Index

        for(int i = 0; i < 4; i++) {
            if(i == 1) packet.Write((ushort)0x1D); // Sei lá o que diabos é isto
            else {
                packet.Write((ushort)0); // TO-DO: Effects?
            }
        }

        packet.Write((byte)0); // Unk
        packet.Write((byte)0); // TO-DO: ChaosPoints

        packet.Write((long)0); // Null1

        packet.Write((byte)0); // TitleId
        packet.Write((byte)0); // TitleLevel

        packet.Write((ushort)0); // Null2

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine($"OK -> CreateCharacterMob");
    }

    private static void SpawnCharacter(Session session, AccountEntitie account, CharacterEntitie character) {
        var packet = PacketFactory.CreateHeader(0x35E, (ushort)account.Id);

        for(int i = 0; i < 8; i++) {
            packet.Write((ushort)0); // TO-DO: Equips
        }

        packet.Write((float)character.PositionX);
        packet.Write((float)character.PositionY);

        packet.Write((ushort)character.Rotation);

        packet.Write(character.MaxHealth);
        packet.Write(character.MaxMana);
        packet.Write(character.CurrentHealth);
        packet.Write(character.CurrentMana);

        packet.Write((ushort)0); // Unk_0

        packet.Write((ushort)character.Level); // Level

        packet.Write((ushort)character.Level); // Null_0

        packet.Write((ushort)0); // IsService

        for(int i = 0; i < 4; i++) {
            packet.Write((byte)0); // TO-DO: Effects
        }

        packet.Write((byte)0); // SpawnType

        packet.Write((byte)character.Height);
        packet.Write((byte)character.Trunk);
        packet.Write((byte)character.Leg);

        packet.Write((ushort)character.Body);

        packet.Write((byte)0); // Mob Type

        packet.Write((byte)account.Nation); // Nation

        packet.Write((ushort)0x7535); // Mob Name?!

        for(int i = 0; i < 3; i++) {
            packet.Write((ushort)0); // Unk_1
        }

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine("OK -> SpawnCharacter");
    }

}