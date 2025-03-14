using GameServer.Core.Base;
using GameServer.Core.Handlers;
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
            character.OwnerAccountId = (ushort)(stream.ReadUInt32());
            var charSlot = (byte)(stream.ReadUInt16());
            var charName = Encoding.ASCII.GetString(stream.ReadBytes(16)).Trim('\0');
            var charClass = (byte)(stream.ReadUInt16());
            var charHair = stream.ReadUInt16();
            _ = Encoding.ASCII.GetString(stream.ReadBytes(12)).TrimEnd('\0');
            var startPositionChoice = (byte)(stream.ReadUInt32());

            if(!ValidateSlot(charSlot, session)) {
                CharacterHandler.GameMessage(session, 16, 0, "Slot Erro!");
                return null; 
            }

            if(!ValidateCharacterName(charName, session)) {
                CharacterHandler.GameMessage(session, 16, 0, "Somente Alfanumericos!");
                return null;
            }
            
            if(!GetValidatedClassInfo(charClass, session)) {
                CharacterHandler.GameMessage(session, 16, 0, "Classe Invalida!");
                return null;
            }

            var classValue = GetCharClassInfo(charClass);
            if(classValue > 5) return null;

            SetCharInitAttributesAndItens(character, classValue);

            if(charHair < 7700 || charHair > 7731)
                CharacterHandler.GameMessage(session, 16, 0, "Voce ta tentando fazer o que?");

            SetCharAppearance(character, classValue, charHair);

            SetCharInitPosition(character, startPositionChoice);

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

    public static bool ValidateSlot(byte slot, Session session) {
        if(slot > 2 || session.ActiveAccount.Characters[slot] != null) {
            return false;
        }

        return true;
    }

    private static bool ValidateCharacterName(string name, Session session) {
        if(name.Length > 14 || !Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9]*$")) {
            return false;
        }

        return true;
    }

    private static bool GetValidatedClassInfo(byte classInfoValue, Session session) {
        if(classInfoValue < 10 || classInfoValue > 69) {
            return false;
        }
        return true;
    }

    private static void SetCharAppearance(CharacterEntitie character, byte classInfo, uint hair) {
        var classMapping = new Dictionary<byte, (ushort itemId, byte classInfo)> {
            {0, (10, 1)},
            {1, (20, 11)},
            {2, (30, 21)},
            {3, (40, 31)},
            {4, (50, 41)},
            {5, (60, 51)}
        };

        if(!classMapping.TryGetValue(classInfo, out var classData)) return;

        character.ClassInfo = classData.classInfo;
        character.Equips[0] = new ItemEntitie { Slot = 0, SlotType = 0, ItemId = classData.itemId };
        character.Equips[1] = new ItemEntitie { Slot = 1, SlotType = 0, ItemId = hair };
    }

    private static void SetCharInitPosition(CharacterEntitie character, uint locationChoice) {
        var positions = new Dictionary<uint, Position> {
            {0, new(3450, 690)}, // Regenshein
            {1, new(3470, 935)} // Verband
        };

        if(positions.TryGetValue(locationChoice, out var position)) { character.Position = position; }
        else return;
    }

    private static void SetCharInitAttributesAndItens(CharacterEntitie character, byte classInfo) {
        if(CharacterRepository.InitialClassItensAndStatus.TryGetValue(classInfo, out var classConfig)) {
            character.Strength = classConfig.Strength;
            character.Intelligence = classConfig.Intelligence;
            character.Agility = classConfig.Agility;
            character.Constitution = classConfig.Constitution;
            character.Luck = classConfig.Luck;

            foreach(var item in classConfig.Items) {
                character.Equips[item.Slot] = item;
            }
        }
    }

    private static byte GetCharClassInfo(byte classInfo) {
        return classInfo switch {
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
        var orderedEquips = GetCharLobbyEquipsOrdered(character?.Equips);

        for(int i = 0; i < 8; i++) {
            stream.Write((ushort)orderedEquips[i].ItemId);
        }
    }

    public static void SetCharEquipsOrdered(CharacterEntitie character, StreamHandler stream) {
        var orderedEquips = GetCharEquipsOrdered(character?.Equips);

        for(int i = 0; i < 16; i++) {
            stream.Write((ushort)orderedEquips[i].ItemId);
        }
    }

    public static void SetCharInventoryOrdered(CharacterEntitie character, StreamHandler stream) {
        var orderedInventory = GetCharInventoryOrdered(character?.Inventory);

        for(int i = 0; i < 60; i++) {
            stream.Write((ushort)orderedInventory[i].ItemId);
        }
    }

    private static Dictionary<int, ItemEntitie> OrderItems(List<ItemEntitie> items, int maxSlots) {
        var orderedItems = Enumerable.Range(0, maxSlots).ToDictionary(i => i, _ => new ItemEntitie());

        foreach(var item in items ?? []) {
            if(item.Slot >= 0 && item.Slot < maxSlots) orderedItems[item.Slot] = item;
        }

        return orderedItems;
    }

    public static Dictionary<int, ItemEntitie> GetCharInventoryOrdered(List<ItemEntitie> inventory)
    => OrderItems(inventory, 60);

    public static Dictionary<int, ItemEntitie> GetCharEquipsOrdered(List<ItemEntitie> equips)
        => OrderItems(equips, 16);

    public static Dictionary<int, ItemEntitie> GetCharLobbyEquipsOrdered(List<ItemEntitie> equips)
        => OrderItems(equips, 8);

    public static void SetCurrentNeighbors(CharacterEntitie character) {
        character.Neighbors.Clear();

        float x = character.PositionX;
        float y = character.PositionY;

        character.Neighbors.Add(new(x - 0.6f, y - 0.6f));
        character.Neighbors.Add(new(x + 0.6f, y + 0.6f));
        character.Neighbors.Add(new(x - 0.7f, y - 0.7f));
        character.Neighbors.Add(new(x + 0.7f, y + 0.7f));
        character.Neighbors.Add(new(x - 0.5f, y - 0.5f));
        character.Neighbors.Add(new(x + 0.5f, y + 0.5f));
        character.Neighbors.Add(new(x - 0.8f, y - 0.8f));
        character.Neighbors.Add(new(x + 0.8f, y + 0.8f));
        character.Neighbors.Add(new(x - 1.0f, y - 1.0f));
    }
}