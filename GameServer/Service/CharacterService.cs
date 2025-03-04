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
    private static void SetCharAppearance(CharacterEntitie character, byte classInfo, uint hair) {
        ItemEntitie classInfoItem = new() {
            Slot = 0,
            SlotType = 0,
        };

        ItemEntitie hairItem = new() {
            Slot = 1,
            SlotType = 0,
            ItemId = hair,
        };

        switch(classInfo) {
            case 0:
            classInfoItem.ItemId = 10;
            character.ClassInfo = 1;
            break;
            case 1:
            classInfoItem.ItemId = 20;
            character.ClassInfo = 11;
            break;
            case 2:
            classInfoItem.ItemId = 30;
            character.ClassInfo = 21;
            break;
            case 3:
            classInfoItem.ItemId = 40;
            character.ClassInfo = 31;
            break;
            case 4:
            classInfoItem.ItemId = 50;
            character.ClassInfo = 41;
            break;
            case 5:
            classInfoItem.ItemId = 60;
            character.ClassInfo = 51;
            break;
        }

        character.Equips[0] = classInfoItem;
        character.Equips[1] = hairItem;
    }

    private static void SetCharInitPosition(CharacterEntitie character, uint local) {
        if(local == 0) { // Regenshien
            Position position = new(3450, 690);
            character.Position = position;
        }
        else if(local == 1) { // Verband
            Position position = new(3470, 935);
            character.Position = position;
        }
    }

    private static void SetCharInitAttributesAndItens(CharacterEntitie character, byte classInfo) {
        if(CharacterRepository.InitialClassItensAndStatus.TryGetValue(classInfo, out var classConfig)) {
            character.Strength = (uint)classConfig.Strength;
            character.Intelligence = (uint)classConfig.Intelligence;
            character.Agility = (uint)classConfig.Agility;
            character.Constitution = (uint)classConfig.Constitution;
            character.Luck = (uint)classConfig.Luck;

            foreach(var item in classConfig.Items) {
                character.Equips[item.Slot] = item;
            }
        }
    }

    private static byte GetCharClassInfo(byte value) {
        return value switch {
            >= 10 and <= 19 => 0,
            >= 20 and <= 29 => 1,
            >= 30 and <= 39 => 2,
            >= 40 and <= 49 => 3,
            >= 50 and <= 59 => 4,
            >= 60 and <= 69 => 5,
            _ => throw new ArgumentException("Classe inválida")
        };
    }

    public static async Task<bool> CreateCharAsync(CharacterEntitie character, AccountEntitie account) {
        return await CharacterRepository.CreateCharacterAsync(character, account);
    }

    public static CharacterEntitie GenerateInitCharacter(Session session, StreamHandler stream) {
        try {
            var character = new CharacterEntitie {
                OwnerAccountId = BitConverter.ToUInt32(stream.ReadBytes(4), 0),
                Inventory = Enumerable.Range(0, 60).Select(_ => new ItemEntitie()).ToList(),
                Equips = Enumerable.Range(0, 8).Select(_ => new ItemEntitie()).ToList(),
                SpeedMove = 40,
                Height = 7,
                Trunk = 119,
                Leg = 119,
                Body = 0
            };

            var slot = stream.ReadBytes(4)[0];
            if(slot > 2) {
                CharacterHandler.GameMessage(session, 16, 0, "SLOT_ERROR");
                return null;
            }

            character.Slot = slot;

            var name = Encoding.ASCII.GetString(stream.ReadBytes(16)).Trim('\0'); ;
            if(name.Length > 14 || !Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9]*$")) {
                CharacterHandler.GameMessage(session, 16, 0, "SOMENTE ALFANUMERICOS!");
                return null;
            }

            character.Name = name;

            var classInfoValue = stream.ReadBytes(2)[0];
            if(classInfoValue < 10 || classInfoValue > 69) {
                CharacterHandler.GameMessage(session, 16, 0, "CLASSE INVALIDA!");
                return null;
            }

            var classInfo = GetCharClassInfo(classInfoValue);

            SetCharInitAttributesAndItens(character, classInfo);

            var hair = BitConverter.ToUInt16(stream.ReadBytes(2), 0);
            if(hair < 7700 || hair > 7731)
                CharacterHandler.GameMessage(session, 16, 0, "Cabelo fora dos limites");

            SetCharAppearance(character, classInfo, hair);

            _ = Encoding.ASCII.GetString(stream.ReadBytes(12)).TrimEnd('\0');

            var local = BitConverter.ToUInt32(stream.ReadBytes(4), 0);
            SetCharInitPosition(character, local);

            character.CurrentHealth = 120;
            character.CurrentMana = 120;

            return character;
        }
        catch {
            return null;
        }
    }

    public static void SetCharLobbyOrdered(CharacterEntitie character, StreamHandler stream) {
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

    public static Dictionary<int, ItemEntitie> GetCharLobbyEquipsOrdered(List<ItemEntitie> equips) {
        var orderedEquips = new Dictionary<int, ItemEntitie>();

        for(int i = 0; i < 8; i++) {
            orderedEquips[i] = new ItemEntitie();
        }

        foreach(var equip in equips ?? []) {
            if(equip.Slot >= 0 && equip.Slot < 8) {
                orderedEquips[equip.Slot] = equip;
            }
        }

        return orderedEquips;
    }

    public static Dictionary<int, ItemEntitie> GetCharEquipsOrdered(List<ItemEntitie> equips) {
        var orderedEquips = new Dictionary<int, ItemEntitie>();

        for(int i = 0; i < 16; i++) {
            orderedEquips[i] = new ItemEntitie();
        }

        foreach(var equip in equips ?? []) {
            if(equip.Slot >= 0 && equip.Slot < 16) {
                orderedEquips[equip.Slot] = equip;
            }
        }

        return orderedEquips;
    }

    public static Dictionary<int, ItemEntitie> GetCharInventoryOrdered(List<ItemEntitie> itens) {
        var orderedInventory = new Dictionary<int, ItemEntitie>();

        for(int i = 0; i < 60; i++) {
            orderedInventory[i] = new ItemEntitie();
        }

        foreach(var item in itens ?? []) {
            if(item.Slot >= 0 && item.Slot < 60) {
                orderedInventory[item.Slot] = item;
            }
        }

        return orderedInventory;
    }

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