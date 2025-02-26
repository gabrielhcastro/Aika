using GameServer.Core.Base;
using GameServer.Core.Handlers;
using GameServer.Data.Repositories;
using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Model.Item;
using System.Text;
using System.Text.RegularExpressions;

namespace GameServer.Service;
public static class CharacterService {
    private static void SetAppearance(CharacterEntitie character, byte classInfo, uint hair) {
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

    private static void SetInitialPosition(CharacterEntitie character, uint local) {
        if(local == 0) { // Regenshien
            character.PositionX = 3450;
            character.PositionY = 690;
        }
        else if(local == 1) { // Verband
            character.PositionX = 3470;
            character.PositionY = 935;
        }
    }

    private static void SetInitialAttributesAndItens(CharacterEntitie character, byte classInfo) {
        if(CharacterRepository.InitialClassItensAndStatus.TryGetValue(classInfo, out var classConfig)) {
            character.Strength = (uint)classConfig.Strength;
            character.Intelligence = (uint)classConfig.Intelligence;
            character.Agility = (uint)classConfig.Agility;
            character.Constitution = (uint)classConfig.Constitution;
            character.Luck = (uint)classConfig.Luck;

            foreach(var item in classConfig.Items) {
                Console.WriteLine($"Adicionando item inicial -> Slot: {item.Slot}, ItemId: {item.ItemId}");
                EquipCharacter(character, item);
            }
        }
    }

    // SE ALGO DER ERRADO COM APARENCIA DO PERSONAGEM FOI PQ EU MEXI AQUI
    private static byte GetClassInfo(byte value) {
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

    private static void EquipCharacter(CharacterEntitie character, ItemEntitie item) {
        character.Equips[item.Slot] = item;
    }

    public static CharacterEntitie GenerateInitialCharacter(Session session, StreamHandler stream) {
        try {
            var character = new CharacterEntitie {
                OwnerAccountId = BitConverter.ToUInt32(stream.ReadBytes(4), 0),
                Itens = Enumerable.Range(0, 60).Select(_ => new ItemEntitie()).ToList(),
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

            var classInfo = GetClassInfo(classInfoValue);

            SetInitialAttributesAndItens(character, classInfo);

            var hair = BitConverter.ToUInt16(stream.ReadBytes(2), 0);
            if(hair < 7700 || hair > 7731)
                CharacterHandler.GameMessage(session, 16, 0, "Cabelo fora dos limites");

            SetAppearance(character, classInfo, hair);
            
            _ = Encoding.ASCII.GetString(stream.ReadBytes(12)).TrimEnd('\0');

            var local = BitConverter.ToUInt32(stream.ReadBytes(4), 0);
            SetInitialPosition(character, local);

            character.CurrentHealth = 120;
            character.CurrentMana = 120;

            return character;
        }
        catch {
            return null;
        }
    }

    public static async Task<bool> CreateCharacterAsync(CharacterEntitie character, AccountEntitie account) {
        return await CharacterRepository.CreateCharacterAsync(character, account);
    }
}