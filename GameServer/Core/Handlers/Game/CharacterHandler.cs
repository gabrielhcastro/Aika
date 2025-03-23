using GameServer.Core.Base;
using GameServer.Core.Handlers.Core;
using GameServer.Core.Handlers.Game;
using GameServer.Data.Repositories;
using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Model.World;
using GameServer.Network;
using GameServer.Service;
using System.IO;
using System.Text;

namespace GameServer.Core.Handlers.InGame;
public static class CharacterHandler {
    public static async Task SendToCharactersList(Session session) {
        var account = session.ActiveAccount;
        session.ActiveAccount.Characters = await CharacterRepository.GetCharactersByAccountIdAsync(account.Id);

        var packet = CharacterService.CreateCharactersListPacket(account);
        session.SendPacket(packet);
    }

    // TO-DO: Mapear Personagem (Item, Buffs...)
    public static void SendToWorld(Session session, StreamHandler stream) {
        var characterSlot = stream.ReadBytes(4)[0];
        //var numericRequestChange = stream.ReadBytes(4)[0];
        //var numeric1 = Encoding.ASCII.GetString(stream.ReadBytes(4));
        //var numeric2 = Encoding.ASCII.GetString(stream.ReadBytes(4));

        var account = session.ActiveAccount;
        var character = account.Characters.FirstOrDefault(c => c.Slot == characterSlot);
        if(character == null) return;

        SendData(session, 0xCCCC, 1);
        SendData(session, 0x186, 1);
        SendData(session, 0x186, 1);
        SendData(session, 0x186, 1);
        SendClientIndex(session, account.ConnectionId);

        var packet = CharacterService.CreateSendToWorldPacket(character, account.ConnectionId, account.Nation);
        session.SendPacket(packet);

        session.ActiveCharacter = character;

        SessionHandler.AddChar(character);
    }

    public static async Task SendToWorldSends(Session session) {
        var character = session.ActiveCharacter;
        if(character == null) {
            GameMessage(session, 16, 0, "Erro ao carregar personagem");
            await SendToCharactersList(session);
            return;
        }

        if(character.IsActive) return;
        
        session.ActiveCharacter.IsActive = true;

        character.Neighbors = [];
        character.VisiblePlayers = [];
        character.VisibleMobs = [];
        character.VisibleNpcs = [];
        character.Position = new(character.PositionX, character.PositionY);

        SendCurrentLevelAndXp(session);
        SendStatus(session);
        SendHpMp(session);
        SendAttributes(session);

        Teleport(session, character.Position.X, character.Position.Y);

        CreateMob(session, session.ActiveCharacter, 1, 0); // Self Character

        GridHandler.Instance.AddCharacter(session.ActiveCharacter);
        SessionHandler.Instance.UpdateVisibleList(session);

        foreach(var equip in character.Equips) ItemHandler.UpdateItemBySlotAndSlotType(session, equip, true);

        foreach(var item in character.Inventory) ItemHandler.UpdateItemBySlotAndSlotType(session, item, true);
    }

    public static void Teleport(Session session, float positionX, float positionY) {
        var packet = CharacterService.CreateTeleportPacket(positionX, positionY);
        session.SendPacket(packet);

        session.ActiveCharacter.Position.X = positionX;
        session.ActiveCharacter.Position.Y = positionY;
    }

    private static void SendData(Session session, ushort packetCode, uint data) {
        var packet = CharacterService.CreateSendDataPacket(session, packetCode, data);
        session.SendPacket(packet);
    }

    private static void SendClientIndex(Session session, uint clientId) {
        var packet = CharacterService.CreateSendClientIndexPacket(clientId);
        session.SendPacket(packet);
    }

    public static void SendHpMp(Session session) {
        var packet = CharacterService.SendHpMpPacket(session.ActiveCharacter);
        session.SendPacket(packet);
    }

    private static void SendAttributes(Session session) {
        var packet = CharacterService.CreateSendAttributesPacket(session.ActiveCharacter);
        session.SendPacket(packet);
    }

    private static void SendCurrentLevelAndXp(Session session) {
        var packet = CharacterService.CreateSendLvlAndXpPacket(session.ActiveCharacter);
        session.SendPacket(packet);
    }

    // TO-DO: Calcular Status
    private static void SendStatus(Session session) {
        var packet = CharacterService.CreateSendStatusPacket(session.ActiveCharacter);
        session.SendPacket(packet);
    }

    // TO-DO:
    // VERIFICAR SE TA BANIDO
    // VERIFICAR DIAS DE BAN CASO BANIDO
    // VERIFICAR GOLD DO BAU
    // VERIFICAR CASH DA CONTA
    // VERIFICAR TEMPO DE EXPIRAÇÃO DO PREMIUM
    // VERIFICAR NAÇÃO
    // VERIFICAR O STATUS/TIPO DA CONTA
    public static async Task SelectedNation(Session session, StreamHandler stream) {
        uint connectionId = stream.ReadUInt32();
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');

        Console.WriteLine($"ConnectionId: {connectionId}");
        Console.WriteLine($"Username: {username}");

        var account = await AccountRepository.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        account.ConnectionId = (ushort)connectionId;
        session.ActiveAccount = account;

        await SendToCharactersList(session);
    }

    public static async Task CreateChar(Session session, StreamHandler stream) {
        try {
            var character = CharacterService.GenerateInitCharacter(session, stream);

            if(character == null || string.IsNullOrWhiteSpace(character.Name)) {
                GameMessage(session, 16, 0, "Personagem Invalido!");
                return;
            }

            var account = session.ActiveAccount;
            if(account == null) return;

            var success = await CharacterService.CreateCharAsync(character, account);
            if(!success) {
                GameMessage(session, 16, 0, "Erro ao criar o personagem!");
                return;
            }

            character.Inventory = [];
            account.Characters.Add(character);

            Console.WriteLine($"Personagem criado: {character.Name}");

            await SendToCharactersList(session);
        }
        catch(Exception ex) {
            Console.WriteLine($"Erro ao criar personagem: {ex.Message}");
            GameMessage(session, 16, 0, "Erro ao criar o personagem!");
        }
    }

    public static async Task ChangeChar(Session session) {
        var account = session.ActiveAccount;
        var character = session.ActiveCharacter;

        if(account == null || character == null) SessionHandler.Instance.RemoveSession(session);

        character.IsActive = false;
        character.VisiblePlayers.Clear();
        SessionHandler.RemoveChar((ushort)character.Id);

        await SendToCharactersList(session);
    }

    public static void GameMessage(Session session, byte type1, byte type2, string message) {
        var packet = CharacterService.CreateGameMessagePacket(type1, type2, message);
        session.SendPacket(packet);
    }

    public static void UpdateRotation(Session session, StreamHandler stream) {
        var account = session.ActiveAccount;

        var rotation = stream.ReadBytes(4);
        var parsedRotation = BitConverter.ToUInt16(rotation);

        if(session.ActiveCharacter.Rotation != parsedRotation)
            session.ActiveCharacter.Rotation = parsedRotation;

        var packet = CharacterService.CreateUpdateRotationPacket(account.ConnectionId, parsedRotation);

        UpdateToAll(session, packet);
    }

    private static void UpdateToAll(Session session, byte[] packet) {
        if(session.ActiveCharacter == null) return;

        var character = session.ActiveCharacter;

        var nearbyCharacters = GridHandler.Instance.GetNearbyCharacters(character.Position, 25);

        foreach(var nearbyCharacter in nearbyCharacters) {
            var nearbySession = SessionHandler.Instance.GetSessionByCharId((ushort)nearbyCharacter.Id);

            if(nearbySession == null) continue;

            if(!character.VisiblePlayers.Contains((ushort)nearbyCharacter.Id)) 
                SessionHandler.Instance.UpdateVisibleList(session);

            nearbySession.SendPacket(packet);
        }
    }

    public static void CreateMob(Session session, CharacterEntitie character, ushort id, byte spawnType) {
        if(session == null) return;

        var packet = CharacterService.CreateCharacterMobPacket(character, id, spawnType);
        session.SendPacket(packet);
    }

    public static void SpawnMob(Session session, byte spawnType) {
        var account = session.ActiveAccount;
        var character = session.ActiveCharacter;

        var packet = CharacterService.CreateSpawnMobPacket(character, 1);
        session.SendPacket(packet);
    }

    // TO-DO: Tratar player morto
    public static void MoveChar(Session session, StreamHandler stream) {
        var destinationX = stream.ReadSingle();
        var destinationY = stream.ReadSingle();
        _ = stream.ReadBytes(6);
        var moveType = stream.ReadByte();
        var speed = stream.ReadByte();
        var unk = stream.ReadUInt32();

        var account = session.ActiveAccount;
        var packet = CharacterService.CreateMovMobPacket(account.ConnectionId, destinationX, destinationY, moveType, speed, unk);
        session.SendPacket(packet);
        UpdateToAll(session, packet);

        session.ActiveCharacter.Position.X = destinationX;
        session.ActiveCharacter.Position.Y = destinationY;
    }

    internal static void UpdateCharInfo(Session session, StreamHandler stream) {
        //var index = stream.ReadUInt32();
        //var loop = stream.ReadUInt32();

        //var packet = PacketFactory.CreateHeader(0x306, 1);
        //packet.Write(index);
        //packet.Write(loop);

        //PacketFactory.FinalizePacket(packet);

        //var packetData = packet.GetBytes();
        //EncDec.Encrypt(ref packetData, packetData.Length);

        //session.SendPacket(packetData);

        //PacketPool.Return(packet);

        Console.WriteLine($"UpdateCharInfo Data: {stream.GetBytes()}");
    }

    public static void ChatMessage(Session session, StreamHandler stream) {
        var chatType = stream.ReadUInt16();
        _ = stream.ReadBytes(6);
        var messageColor = stream.ReadUInt32();
        var charName = Encoding.ASCII.GetString(stream.ReadBytes(16)).TrimEnd('\0');
        var message = Encoding.ASCII.GetString(stream.ReadBytes(128)).TrimEnd('\0');

        var account = session.ActiveAccount;

        var isCommand = message.StartsWith('.');

        if(isCommand) {
            var parts = message.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length == 0) return;

            var command = parts[0].ToLower();

            switch(command) {
                case "teleport":
                if(parts.Length < 3) return;
                if(float.TryParse(parts[1], out float posX) && float.TryParse(parts[2], out float posY)) Teleport(session, posX, posY);
                else return;
                break;
                case "spawn":
                if(parts.Length < 4) return;
                //if(byte.TryParse(parts[2], out byte mobId) && byte.TryParse(parts[3], out byte spawnType)) SpawnCharacter(session, spawnType);
                //else return;
                break;
            }
        }
        else {
            var packet = CharacterService.CreateChatMessagePacket(account.ConnectionId, chatType, messageColor, charName, message);
            session.SendPacket(packet);
        }
    }

    public static void RemoveCharacterMob(Session otherSession, uint id) {
        Console.WriteLine($"Tentando remover {otherSession.ActiveCharacter.Name}");
    }
}