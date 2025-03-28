using GameServer.Model.Character;
using GameServer.Model.Mob;
using GameServer.Network;
using System.Text;

namespace GameServer.Service;
internal class MobService {
    internal static byte[] CreateCharacterMobPacket(CharacterEntitie character, ushort id, byte spawnType) {
        var packet = PacketFactory.CreateHeader(0x349, id);
        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0')));
        ItemService.SetLobbyEquips(character, packet);
        packet.Write(new byte[12]); // Item Effect?
        packet.Write((float)character.Position.X);
        packet.Write((float)character.Position.Y);
        packet.Write((uint)0); // Provavelmente a nação
        //packet.Write(character.MaxHealth);
        //packet.Write(character.MaxMana);
        packet.Write(character.CurrentHealth);
        packet.Write(character.CurrentMana);
        packet.Write(character.CurrentHealth);
        packet.Write(character.CurrentMana);
        packet.Write((byte)0xA); // Unk
        packet.Write(character.SpeedMove);
        packet.Write((byte)spawnType); // SpawnType (0: Normal, 1: Teleporte, 2: BabyGen)
        packet.Write(character.Height);
        packet.Write(character.Trunk);
        packet.Write(character.Leg);
        packet.Write(character.Body);
        packet.Write((byte)0); // Unk
        packet.Write((byte)0); // Unk
        packet.Write((byte)0); // IsService (0 Player, 1 Npc?!)
        packet.Write((ushort)0); // TO-DO: EffectType
        packet.Write((ushort)0); // TO-DO: SetBuffs
        for(ushort i = 0; i < 60; i++) packet.Write((ushort)0); // TO-DO: Buffs
        for(ushort i = 0; i < 60; i++) packet.Write((uint)0); // TO-DO: BuffTime
        packet.Write(Encoding.ASCII.GetBytes(new string('\0', 32))); // Title
        packet.Write((ushort)0); // Unk
        packet.Write((byte)6); // Unk
        packet.Write((byte)0); // Unk
        packet.Write((ushort)0); // Unk
        //packet.Write((ushort)account.Nation * 4096); 
        packet.Write((uint)0);
        packet.Write((uint)0);
        packet.Write((byte)0); // Unk
        packet.Write((byte)0xFF); // TO-DO: ChaosPoints
        packet.Write((byte)0); // TitleId
        packet.Write((byte)0); // TitleLevel

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateSpawnMobPacket(MobEntitie mob) {
        var packet = PacketFactory.CreateHeader(0x35E, (ushort)(mob.Index + 3048));
        foreach(var equip in mob.Equips) {
            packet.Write(equip);
        }
        packet.Write(mob.Position.X);
        packet.Write(mob.Position.Y);
        packet.Write((uint)mob.Rotation);
        //packet.Write(character.MaxHealth);
        //packet.Write(character.MaxMana);
        packet.Write(mob.InitialHealth);
        packet.Write(mob.InitialHealth);
        packet.Write(mob.InitialHealth);
        packet.Write(mob.InitialHealth);
        packet.Write((ushort)0); // Unk_1
        packet.Write((ushort)mob.MobLevelDifference); // Level Difference (gray, blue, yellow, orange, purple)
        packet.Write((ushort)0); // Null_0
        packet.Write(mob.IsService ? (ushort)1 : (ushort)0);
        packet.Write(new byte[4]); // Effects?
        packet.Write((byte)mob.SpawnType); // Spawn Type
        packet.Write(mob.Height);
        packet.Write(mob.Trunk);
        packet.Write(mob.Leg);
        packet.Write((ushort)0); // Body
        packet.Write((byte)mob.MobType); // Mob Type
        packet.Write((byte)0); // Nation
        packet.Write((ushort)mob.NameValue); // Mob Name
        for(byte i = 0; i < 3; i++) packet.Write((ushort)0); // Unk

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateMovMobPacket(ushort connectionId, Single destinationX, Single destinationY, byte moveType, byte speed, uint unk) {
        var packet = PacketFactory.CreateHeader(0x301, connectionId);
        packet.Write(destinationX);
        packet.Write(destinationY);
        packet.Write((uint)0); // Null
        packet.Write((ushort)0); // Null
        packet.Write(moveType); // moveType
        packet.Write(speed); // Speed
        packet.Write(unk); // unk

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }
}
