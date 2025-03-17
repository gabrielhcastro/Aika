using GameServer.Core.Base;
using GameServer.Core.Handlers.Core;
using GameServer.Data.Repositories;
using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Model.Item;
using GameServer.Model.World;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace GameServer.Service;
public static class CharacterService {
    public static async Task<bool> CreateCharAsync(CharacterEntitie character, AccountEntitie account) {
        return await CharacterRepository.CreateCharacterAsync(character, account);
    }

    public static CharacterEntitie GenerateInitCharacter(Session session, StreamHandler stream) {
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

    public static void SetLobbyEquips(CharacterEntitie character, StreamHandler stream) {
        var orderedEquips = GetLobbyEquipsOrdered(character?.Equips);

        for(int i = 0; i < 8; i++) {
            stream.Write((ushort)orderedEquips[i].ItemId);
        }
    }

    public static void SetEquipsOrdered(CharacterEntitie character, StreamHandler stream) {
        var orderedEquips = GetEquipsOrdered(character?.Equips);

        for(int i = 0; i < 16; i++) stream.Write((ushort)orderedEquips[i].ItemId);
    }

    public static void SetInventoryOrdered(CharacterEntitie character, StreamHandler stream) {
        var orderedInventory = GetInventoryOrdered(character?.Inventory);

        for(int i = 0; i < 60; i++) stream.Write((ushort)orderedInventory[i].ItemId);
    }

    private static Dictionary<int, ItemEntitie> OrderItems(List<ItemEntitie> items, int maxSlots) {
        var orderedItems = Enumerable.Range(0, maxSlots).ToDictionary(i => i, _ => new ItemEntitie());

        foreach(var item in items ?? []) {
            if(item.Slot >= 0 && item.Slot < maxSlots) orderedItems[item.Slot] = item;
        }

        return orderedItems;
    }

    public static Dictionary<int, ItemEntitie> GetInventoryOrdered(List<ItemEntitie> inventory)
    => OrderItems(inventory, 60);

    public static Dictionary<int, ItemEntitie> GetEquipsOrdered(List<ItemEntitie> equips)
        => OrderItems(equips, 16);

    public static Dictionary<int, ItemEntitie> GetLobbyEquipsOrdered(List<ItemEntitie> equips)
        => OrderItems(equips, 8);
}