using GameServer.Core.Base;
using GameServer.Core.Handlers.Core;
using GameServer.Data.Repositories;
using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Model.Item;
using GameServer.Model.World;
using GameServer.Network;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameServer.Service;
public static class CharacterService {
    private static CharacterEntitie CreateBaseCharacter(StreamHandler stream) {
        return new CharacterEntitie {
            OwnerAccountId = BitConverter.ToUInt16(stream.ReadBytes(4), 0),
            Inventory = Enumerable.Range(0, 60).Select(_ => new ItemEntitie()).ToList(),
            Equips = Enumerable.Range(0, 16).Select(_ => new ItemEntitie()).ToList(),
            SpeedMove = 40,
            Height = 7,
            Trunk = 119,
            Leg = 119,
            Body = 0
        };
    }

    public static bool IsSlotValid(Session session, byte slot) {
        if(slot > 2) return false;

        foreach(var character in session?.ActiveAccount?.Characters) {
            if(character.Slot == slot) return false;
        }

        return true;
    }

    private static bool IsNameValid(string name) {
        if(name.Length > 14 || !Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9]*$")) {
            return false;
        }

        return true;
    }

    private static bool IsClassValid(byte classValue) {
        if(classValue < 10 || classValue > 69) {
            return false;
        }
        return true;
    }

    private static void SetInitAppearance(CharacterEntitie character, byte classInfo, uint hair) {
        var appearancePresets = new Dictionary<byte, (ushort classAppearance, byte classValue)> {
            {0, (10, 1)},
            {1, (20, 11)},
            {2, (30, 21)},
            {3, (40, 31)},
            {4, (50, 41)},
            {5, (60, 51)}
        };

        if(!appearancePresets.TryGetValue(classInfo, out var classData)) return;

        character.ClassInfo = classData.classValue;
        character.Equips[0] = new ItemEntitie { Slot = 0, SlotType = 0, ItemId = classData.classAppearance };
        character.Equips[1] = new ItemEntitie { Slot = 1, SlotType = 0, ItemId = hair };
    }

    private static void SetInitPosition(CharacterEntitie character, uint locationChoice) {
        var positions = new Dictionary<uint, Position> {
            {0, new(3450, 690)}, // Regenshein
            {1, new(3470, 935)} // Verband
        };

        if(positions.TryGetValue(locationChoice, out var position)) { character.Position = position; }
        else return;
    }

    private static void SetInitAttributesAndEquips(CharacterEntitie character, byte classValue) {
        if(CharacterRepository.ClassInitAttributesAndEquips.TryGetValue(classValue, out var classPreset)) {
            character.Strength = classPreset.Strength;
            character.Intelligence = classPreset.Intelligence;
            character.Agility = classPreset.Agility;
            character.Constitution = classPreset.Constitution;
            character.Luck = classPreset.Luck;

            foreach(var item in classPreset.Items) character.Equips[item.Slot] = item;
        }
    }

    private static byte GetClassValue(byte classValue) {
        return classValue switch {
            >= 10 and <= 19 => 0,
            >= 20 and <= 29 => 1,
            >= 30 and <= 39 => 2,
            >= 40 and <= 49 => 3,
            >= 50 and <= 59 => 4,
            >= 60 and <= 69 => 5,
            _ => byte.MaxValue
        };
    }

    internal static async Task<bool> CreateCharAsync(CharacterEntitie character, AccountEntitie account) {
        return await CharacterRepository.CreateCharacterAsync(character, account);
    }

    internal static CharacterEntitie GenerateInitCharacter(Session session, StreamHandler stream) {
        try {
            var character = CreateBaseCharacter(stream);
            var charSlot = stream.ReadBytes(4)[0];
            var charName = Encoding.ASCII.GetString(stream.ReadBytes(16)).Trim('\0');
            var charClass = stream.ReadBytes(2)[0];
            var charHair = BitConverter.ToUInt16(stream.ReadBytes(2), 0);
            _ = Encoding.ASCII.GetString(stream.ReadBytes(12)).TrimEnd('\0');
            var startPositionChoice = BitConverter.ToUInt32(stream.ReadBytes(4), 0);

            if(!IsSlotValid(session, charSlot)) return null; 

            character.Slot = charSlot;

            if(!IsNameValid(charName)) return null;

            character.Name = charName;
            
            if(!IsClassValid(charClass)) return null;

            character.ClassInfo = charClass;

            var classValue = GetClassValue(charClass);
            if(classValue > 5) return null;

            SetInitAttributesAndEquips(character, classValue);

            if(charHair < 7700 || charHair > 7731) return null;

            SetInitAppearance(character, classValue, charHair);

            SetInitPosition(character, startPositionChoice);

            character.CurrentHealth = 120;
            character.CurrentMana = 120;

            return character;
        }
        catch {
            return null;
        }
    }

    internal static byte[] CreateCharactersListPacket(AccountEntitie account) {
        var packet = PacketFactory.CreateHeader(0x901, account.ConnectionId);
        packet.Write((uint)account.Id);
        packet.Write((uint)0); // Unk
        packet.Write((uint)0); // Unk
        CharacterEntitie[] slots = new CharacterEntitie[3];
        foreach(var character in account.Characters) {
            if(character.Slot < 3) {
                slots[character.Slot] = character;
            }
        }
        for(byte i = 0; i < 3; i++) {
            WriteCharacterEntry(packet, account, slots[i]);
        }

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packet.Count);

        return packetData;
    }

    private static void WriteCharacterEntry(StreamHandler packet, AccountEntitie account, CharacterEntitie character) {
        if(character != null) character.IsActive = false;
        packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0') ?? new string('\0', 16)));
        packet.Write((ushort)(account?.Nation ?? 0));
        packet.Write((ushort)(character?.ClassInfo ?? 0));
        packet.Write(character?.Height ?? 0);
        packet.Write(character?.Trunk ?? 0);
        packet.Write(character?.Leg ?? 0);
        packet.Write(character?.Body ?? 0);
        ItemService.SetLobbyEquips(character, packet);
        for(ushort k = 0; k < 12; k++) packet.Write((byte)0); //Refine?
        packet.Write(character?.Strength ?? 0); // Str
        packet.Write(character?.Agility ?? 0); // Agi
        packet.Write(character?.Intelligence ?? 0); // Int
        packet.Write(character?.Constitution ?? 0); // Cons
        packet.Write(character?.Luck ?? 0); // Luck
        packet.Write(character?.Status ?? 0); // Status
        packet.Write((ushort)(character?.Level ?? 65535)); // Level
        packet.Write(new byte[6]); // Null
        packet.Write((long)(character?.Experience ?? 1)); // Exp
        packet.Write((long)(character?.Gold ?? 0)); // Gold
        packet.Write((uint)0); // Unk
        packet.Write((uint)(character?.Deleted ?? 0));
        packet.Write(character?.NumericErrors ?? 0);
        packet.Write((byte)1); // Unk
        packet.Write((uint)0); // Unk
        packet.Write((ushort)0); // Unk
    }

    internal static byte[] CreateSendToWorldPacket(CharacterEntitie character, uint connectionId, byte nation) {
        var packet = PacketFactory.CreateHeader(0x925, 0x7535);
        packet.Write(connectionId); // Serial
        packet.Write((uint)1); // First Login 
        packet.Write((uint)0);
        packet.Write(character.Id);
        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0')));
        packet.Write(nation);
        packet.Write(character.ClassInfo);
        packet.Write(character.Strength);
        packet.Write(character.Agility);
        packet.Write(character.Intelligence);
        packet.Write(character.Constitution);
        packet.Write(character.Luck);
        packet.Write(character.Status);
        packet.Write(character.Height);
        packet.Write(character.Trunk);
        packet.Write(character.Leg);
        packet.Write(character.Body);
        //packet.Write(character.MaxHealth);
        packet.Write(character.CurrentHealth);
        packet.Write(character.CurrentHealth);
        //packet.Write(character.MaxMana);
        packet.Write(character.CurrentMana);
        packet.Write(character.CurrentMana);
        packet.Write((ushort)0); // Server Reset Time
        packet.Write(character.Honor);
        packet.Write(character.KillPoint);
        packet.Write((uint)character.Infamia);
        packet.Write((ushort)0); // TO-DO: Evil Points
        packet.Write((ushort)0); // TO-DO: Skill Points
        packet.Write(new byte[60]); // Null_1
        packet.Write((ushort)0); // Unk_0
        packet.Write(character.PhysicDamage);
        packet.Write(character.PhysicDefense);
        packet.Write(character.MagicDamage);
        packet.Write(character.MagicDefense);
        packet.Write(character.BonusDamage);
        packet.Write(new byte[10]); // Null_2
        packet.Write((ushort)0); // Critical
        packet.Write((byte)0); // Miss
        packet.Write((byte)0); // Accuracy
        packet.Write((uint)0); // Null_3
        packet.Write(character.Experience);
        packet.Write((ushort)character.Level);
        packet.Write((ushort)0); // Guild Index
        packet.Write(new byte[32]); // Null_4
        for(ushort i = 0; i < 20; i++) packet.Write((ushort)0); // BuffsId
        for(ushort i = 0; i < 20; i++) packet.Write((uint)0); // BuffsDuration
        ItemService.SetEquipsOrdered(character, packet);
        packet.Write((uint)0);  // Null_5
        ItemService.SetInventoryOrdered(character, packet);
        packet.Write(character.Gold);
        packet.Write(new byte[192]); // Null_6
        packet.Write(new byte[224]); // Quests
        packet.Write(new byte[212]); // Unk_1
        packet.Write((uint)0);  // Unk_2
        packet.Write((uint)0);  // Location
        packet.Write(new byte[128]); // Unk_3
        packet.Write((uint)DateTime.Parse(character.CreationTime).Ticks);
        packet.Write(new byte[436]); // Unk_4
        packet.Write(Encoding.ASCII.GetBytes("0000"));
        packet.Write(new byte[212]); // Unk_5
        for(ushort i = 0; i < 60; i++) packet.Write((ushort)0); // Skill List
        for(ushort i = 0; i < 24; i++) packet.Write((uint)0); // Item Bar
        packet.Write((uint)0); // Null_7
        for(ushort i = 0; i < 12; i++) packet.Write((uint)0); // TitleCategoryLevel
        packet.Write(new byte[80]); // Unk_4
        packet.Write((ushort)0); // Active Title
        packet.Write((uint)0); // Null_8
        for(ushort i = 0; i < 48; i++) packet.Write((ushort)0); // TitleProgressType8
        for(ushort i = 0; i < 2; i++) packet.Write((ushort)0); // TitleProgressType9
        packet.Write((ushort)0); // TitleProgressType4
        packet.Write((ushort)0); // TitleProgressType10
        packet.Write((ushort)0); // TitleProgressType7
        packet.Write((ushort)0); // TitleProgressType11
        packet.Write((ushort)0); // TitleProgressType12
        packet.Write((ushort)0); // TitleProgressType13
        packet.Write((ushort)0); // TitleProgressType15
        packet.Write((ushort)0); // TitleProgressUnk
        for(ushort i = 0; i < 22; i++) packet.Write((ushort)0); // TitleProgressType16
        packet.Write((ushort)0); // TitleProgressType23
        for(ushort i = 0; i < 120; i++) packet.Write((ushort)0); // TitleProgress
        packet.Write((uint)DateTime.Now.AddDays(1).Ticks); // EndDayTime
        packet.Write((uint)0); // Null_9
        packet.Write((uint)0); // Tempo de caça
        packet.Write(new byte[52]); // Unk_4
        packet.Write((uint)3); // Utc
        packet.Write((uint)DateTime.Now.Ticks); // LoginTime
        packet.Write((uint)DateTime.Now.Ticks); // LoginTime
        packet.Write(new byte[852]); // Unk_4
        packet.Write((uint)0);
        packet.Write(new byte[12]); // Unk_4
        packet.Write(Encoding.ASCII.GetBytes("Pran 1".PadRight(16, '\0'))); //Pran Name
        packet.Write((uint)0); // Unk_12

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateTeleportPacket(float positionX, float positionY) {
        var packet = PacketFactory.CreateHeader(0x301, 1);
        packet.Write(positionX);
        packet.Write(positionY);
        packet.Write(new byte[6]);
        packet.Write((byte)1); // Move Type
        packet.Write((byte)0); // Speed 
        packet.Write((uint)0); // Unk_0

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateSendDataPacket(Session session, ushort packetCode, uint data) {
        var packet = PacketFactory.CreateHeader(packetCode, session.ActiveAccount.ConnectionId);
        packet.Write(data);

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateSendClientIndexPacket(uint clientId) {
        var packet = PacketFactory.CreateHeader(0x117);
        packet.Write(clientId);
        packet.Write((uint)0); // Unk

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] SendHpMpPacket(CharacterEntitie character) {
        var packet = PacketFactory.CreateHeader(0x103, 1);
        packet.Write(character.CurrentHealth);
        packet.Write(character.CurrentHealth);
        //packet.Write(session.ActiveCharacter.MaxHealth);
        packet.Write(character.CurrentMana);
        packet.Write(character.CurrentMana);
        //packet.Write(session.ActiveCharacter.MaxMana);
        packet.Write((uint)0); // Null

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateSendAttributesPacket(CharacterEntitie character) {
        var packet = PacketFactory.CreateHeader(0x109, 0x7535);
        packet.Write(character.Strength);
        packet.Write(character.Agility);
        packet.Write(character.Intelligence);
        packet.Write(character.Constitution);
        packet.Write(character.Luck);
        packet.Write(character.Status);
        packet.Write(character.Status);
        packet.Write(character.SkillPoint);

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateSendLvlAndXpPacket(CharacterEntitie character) {
        var packet = PacketFactory.CreateHeader(0x108, 0x7535);
        packet.Write((ushort)character.Level); // Level
        packet.Write((byte)0xCC); // Unk
        packet.Write((byte)0); // Unk
        packet.Write(character.Experience); // XP
        packet.Write(new byte[8]); // Unk

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateSendStatusPacket(CharacterEntitie character) {
        var packet = PacketFactory.CreateHeader(0x10A, 0x7535);
        packet.Write(character.PhysicDamage); // Physic Damage
        packet.Write(character.PhysicDefense); // Physic Defense
        packet.Write(character.MagicDamage); // Magic Damage
        packet.Write(character.MagicDefense); // Magic Defense
        packet.Write(character.BonusDamage); // Bonus
        packet.Write(new byte[10]); // Unk
        packet.Write((ushort)character.SpeedMove); // Speed Move
        packet.Write((ulong)0); // Unk
        packet.Write((ushort)1); // Critical
        packet.Write((ushort)0); // Unk
        packet.Write((ushort)3); // Miss
        packet.Write((ushort)12); // Accuracy
        packet.Write((ushort)2); // Double
        packet.Write((ushort)4); // Resistence
        packet.Write((ushort)0); // Unk

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateGameMessagePacket(byte type1, byte type2, string message) {
        var packet = PacketFactory.CreateHeader(0x984);
        packet.Write((byte)0); // Null1
        packet.Write(type1); // Type1
        packet.Write(type2); // Type2
        packet.Write((byte)0); // Null2
        packet.Write(Encoding.ASCII.GetBytes(message?.PadRight(128, '\0')));

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

    internal static byte[] CreateUpdateRotationPacket(ushort connectionId, ushort rotation) {
        var packet = PacketFactory.CreateHeader(0x305, connectionId);
        packet.Write((uint)rotation);

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }

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

    internal static byte[] CreateSpawnMobPacket(CharacterEntitie character, byte spawnType) {
        var packet = PacketFactory.CreateHeader(0x35E, 1);
        ItemService.SetLobbyEquips(character, packet);
        packet.Write(character.Position.X);
        packet.Write(character.Position.Y);
        packet.Write(character.Rotation);
        //packet.Write(character.MaxHealth);
        packet.Write(character.CurrentHealth);
        packet.Write(character.CurrentHealth);
        //packet.Write(character.MaxMana);
        packet.Write(character.CurrentMana);
        packet.Write(character.CurrentMana);
        packet.Write((ushort)0); // Unk_1
        packet.Write((ushort)character.Level);
        packet.Write((ushort)0); // Null_0
        packet.Write((ushort)1); // IsService
        packet.Write(new byte[4]); // Effects?
        packet.Write(spawnType); // Spawn Type
        packet.Write(character.Height);
        packet.Write(character.Trunk);
        packet.Write(character.Leg);
        packet.Write((ushort)character.Body);
        packet.Write((byte)0); // Mob Type
        packet.Write((byte)0); // Nation
        packet.Write((ushort)0); // Mob Name
        packet.Write(new byte[3]); // Unk_2

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

    internal static byte[] CreateChatMessagePacket(ushort connectionId, ushort chatType, uint messageColor, string charName, string message) {
        var packet = PacketFactory.CreateHeader(0xF86, connectionId);
        if(chatType == 0) packet.Write((uint)8);
        else packet.Write((uint)chatType);
        packet.Write((uint)0); // null_0
        packet.Write(messageColor);
        packet.Write(Encoding.ASCII.GetBytes(charName.Trim().PadRight(16, '\0')));
        packet.Write(Encoding.ASCII.GetBytes(message.Trim().PadRight(128, '\0')));

        PacketFactory.FinalizePacket(packet);
        PacketPool.Return(packet);
        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        return packetData;
    }
}