using GameServer.Core.Base;
using GameServer.Core.Handlers.Core;
using GameServer.Core.Handlers.Game;
using GameServer.Data.Repositories;
using GameServer.Model.Character;
using GameServer.Model.World;
using GameServer.Network;
using GameServer.Service;
using System.Text;

namespace GameServer.Core.Handlers.InGame;
public static class CharacterHandler {
    public static async Task SendToCharactersList(Session session) {
        var account = session.ActiveAccount;
        session.ActiveAccount.Characters = await CharacterRepository.GetCharactersByAccountIdAsync(account.Id);

        var packet = PacketFactory.CreateHeader(0x901, account.ConnectionId);

        packet.Write((uint)account.Id);
        packet.Write((uint)0); // Unk
        packet.Write((uint)0); // Unk

        for(ushort i = 0; i < 3; i++) {
            var character = i < account.Characters.Count ? account.Characters.ToList()[i] : null;

            if(character != null) character.IsActive = false;
            packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0') ?? new string('\0', 16)));

            packet.Write((ushort)(account?.Nation ?? 0));
            packet.Write((ushort)(character?.ClassInfo ?? 0));

            packet.Write(character?.Height ?? 0);
            packet.Write(character?.Trunk ?? 0);
            packet.Write(character?.Leg ?? 0);
            packet.Write(character?.Body ?? 0);

            CharacterService.SetLobbyEquips(character, packet);

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

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    // TO-DO: Mapear Personagem (Item, Buffs...)
    public static void SendToWorld(Session session, StreamHandler stream) {
        var characterSlot = stream.ReadBytes(4)[0];
        //var numericRequestChange = stream.ReadBytes(4)[0];
        //var numeric1 = Encoding.ASCII.GetString(stream.ReadBytes(4));
        //var numeric2 = Encoding.ASCII.GetString(stream.ReadBytes(4));

        var account = session.ActiveAccount;

        var character = account.Characters[characterSlot];
        if(character == null) return;

        session.ActiveCharacter = account.Characters[characterSlot];

        SendData(session, 0xCCCC, 1);
        SendData(session, 0x186, 1);
        SendData(session, 0x186, 1);
        SendData(session, 0x186, 1);
        SendClientIndex(session, account.ConnectionId);

        var packet = PacketFactory.CreateHeader(0x925, 0x7535);

        // Serial
        packet.Write((uint)account.ConnectionId);

        // TO-DO: First Login 
        packet.Write((uint)1);

        packet.Write((uint)0);
        packet.Write(character.Id);
        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0')));
        packet.Write(account.Nation);
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

        CharacterService.SetEquipsOrdered(character, stream);

        packet.Write((uint)0);  // Null_5

        CharacterService.SetInventoryOrdered(character, stream);

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

        //Pran Name
        packet.Write(Encoding.ASCII.GetBytes("Pran 1".PadRight(16, '\0')));

        packet.Write((uint)0); // Unk_12

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);

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
        SendCurrentHpMp(session);
        SendAttributes(session);

        Teleport(session, character.Position.X, character.Position.Y);

        // Cria o proprio personagem com o index fixo 1
        CreateCharacterMob(session, session.ActiveCharacter, 1, 0);

        GridHandler.Instance.AddCharacter(session.ActiveCharacter);
        SessionHandler.Instance.UpdateSendToWorldVisibleList(session);

        foreach(var equip in character.Equips) ItemHandler.UpdateItemBySlotAndType(session, equip, true);

        foreach(var item in character.Inventory) ItemHandler.UpdateItemBySlotAndType(session, item, true);
    }

    public static void Teleport(Session session, float positionX, float positionY) {
        var packet = PacketFactory.CreateHeader(0x301, 1);

        packet.Write(positionX);
        packet.Write(positionY);

        packet.Write(new byte[6]);

        packet.Write((byte)1); // Move Type
        packet.Write((byte)0); // Speed 
        packet.Write((uint)0); // Unk_0

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        session.ActiveCharacter.Position = new Position(positionX, positionY);
    }

    private static void SendData(Session session, ushort packetCode, uint data) {
        var packet = PacketFactory.CreateHeader(packetCode, session.ActiveAccount.ConnectionId);
        packet.Write(data);

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    private static void SendClientIndex(Session session, uint clientId) {
        var packet = PacketFactory.CreateHeader(0x117);
        packet.Write(clientId);
        packet.Write((uint)0); // Unk

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    public static void SendCurrentHpMp(Session session) {
        var packet = PacketFactory.CreateHeader(0x103, 1);

        packet.Write(session.ActiveCharacter.CurrentHealth);
        packet.Write(session.ActiveCharacter.CurrentHealth);
        //packet.Write(session.ActiveCharacter.MaxHealth);
        packet.Write(session.ActiveCharacter.CurrentMana);
        packet.Write(session.ActiveCharacter.CurrentMana);
        //packet.Write(session.ActiveCharacter.MaxMana);

        packet.Write((uint)0); // Null

        PacketFactory.FinalizePacket(packet);

        byte[] packetBytes = packet.GetBytes();
        EncDec.Encrypt(ref packetBytes, packetBytes.Length);

        session.SendPacket(packetBytes);

        PacketPool.Return(packet);
    }

    private static void SendAttributes(Session session) {
        var character = session.ActiveCharacter;

        var packet = PacketFactory.CreateHeader(0x109, 0x7535);

        packet.Write(character.Strength);
        packet.Write(character.Agility);
        packet.Write(character.Intelligence);
        packet.Write(character.Constitution);
        packet.Write(character.Luck);
        packet.Write(character.Status);
        packet.Write((ushort)20); // Status Point
        packet.Write((ushort)20); // Skill Point

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    private static void SendCurrentLevelAndXp(Session session) {
        var character = session.ActiveCharacter;

        var packet = PacketFactory.CreateHeader(0x108, 0x7535);

        packet.Write((ushort)character.Level); // Level
        packet.Write((byte)0xCC); // Unk
        packet.Write((byte)0); // Unk
        packet.Write(character.Experience); // XP
        packet.Write(new byte[8]); // Unk

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    // TO-DO: Calcular Status
    private static void SendStatus(Session session) {
        var packet = PacketFactory.CreateHeader(0x10A, 0x7535);

        packet.Write((ushort)60); // Physic Damage
        packet.Write((ushort)31); // Physic Defense
        packet.Write((ushort)0); // Magic Damage
        packet.Write((ushort)31); // Magic Defense
        packet.Write((ushort)20); // Status

        packet.Write(new byte[10]); // Unk

        packet.Write((ushort)40); // Speed Move

        packet.Write((ulong)13); // Unk

        packet.Write((ushort)1); // Critical

        packet.Write((ushort)13); // Unk

        packet.Write((ushort)3); // Miss

        packet.Write((ushort)12); // Accuracy
        packet.Write((ushort)0); // Double
        packet.Write((ushort)4); // Resistence

        packet.Write((ushort)0); // Unk

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);
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

        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        session.ActiveCharacter.IsActive = false;

        await SendToCharactersList(session);
    }

    public static void GameMessage(Session session, byte type1, byte type2, string message) {
        var packet = PacketFactory.CreateHeader(0x984);

        packet.Write((byte)0); // Null1
        packet.Write(type1); // Type1
        packet.Write(type2); // Type2
        packet.Write((byte)0); // Null2
        packet.Write(Encoding.ASCII.GetBytes(message?.PadRight(128, '\0')));

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    //TO-DO: Deixar visivel para todos/proximos
    public static void UpdateRotation(Session session, StreamHandler stream) {
        var rotation = BitConverter.ToUInt16(stream.ReadBytes(4));

        if(session.ActiveCharacter.Rotation != rotation)
            session.ActiveCharacter.Rotation = rotation;

        var packet = PacketFactory.CreateHeader(0x305, session.ActiveAccount.ConnectionId);
        packet.Write((uint)rotation);

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        UpdateToAll(session, packetData);
    }

    private static void UpdateToAll(Session session, byte[] packet) {
        if(session.ActiveCharacter == null) return;

        var character = session.ActiveCharacter;

        var nearbyCharacters = GridHandler.Instance.GetNearbyCharacters(character.Position, 5);

        foreach(var nearbyCharacter in nearbyCharacters) {
            var nearbySession = SessionHandler.Instance.GetSessionByCharId((ushort)nearbyCharacter.Id);

            if(nearbySession == null) continue;

            nearbySession.SendPacket(packet);
        }
    }

    public static void CreateCharacterMob(Session session, CharacterEntitie character, ushort id, ushort spawnType) {
        if(session == null) return;

        var packet = PacketFactory.CreateHeader(0x349, id);

        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0')));

        CharacterService.SetLobbyEquips(character, packet);

        // Item Effect?
        packet.Write(new byte[12]);

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

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    public static void SpawnCharacter(Session session, byte spawnType) {
        var account = session.ActiveAccount;
        var character = session.ActiveCharacter;

        var packet = PacketFactory.CreateHeader(0x35E, 1);

        CharacterService.SetLobbyEquips(character, packet);

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

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    // TO-DO: Tratar player morto
    public static void MoveChar(Session session, StreamHandler stream) {
        var destinationX = stream.ReadSingle();
        var destinationY = stream.ReadSingle();
        _ = stream.ReadBytes(6);
        var moveType = stream.ReadByte();
        var speed = stream.ReadByte();
        var unk = stream.ReadUInt32();

        var packet = PacketFactory.CreateHeader(0x301, session.ActiveAccount.ConnectionId);

        packet.Write(destinationX);
        packet.Write(destinationY);

        packet.Write((uint)0); // Null

        packet.Write((ushort)0); // Null

        packet.Write(moveType); // moveType

        packet.Write(speed); // Speed
        packet.Write(unk); // unk

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packet);

        UpdateToAll(session, packetData);

        PacketPool.Return(packet);
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

    public static void SendSay(Session session, StreamHandler stream) {
        var chatType = stream.ReadUInt16();
        _ = stream.ReadBytes(6);
        var messageColor = stream.ReadUInt32();
        var charName = Encoding.ASCII.GetString(stream.ReadBytes(16)).TrimEnd('\0');
        var message = Encoding.ASCII.GetString(stream.ReadBytes(128)).TrimEnd('\0');

        var packet = PacketFactory.CreateHeader(0xF86, session.ActiveAccount.ConnectionId);
        if(chatType == 0) packet.Write((uint)8);
        else packet.Write((uint)chatType);

        packet.Write((uint)0); // null_0
        packet.Write(messageColor);

        packet.Write(Encoding.ASCII.GetBytes(charName.Trim().PadRight(16, '\0')));
        packet.Write(Encoding.ASCII.GetBytes(message.Trim().PadRight(128, '\0')));

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);

        if(message.StartsWith('.')) {
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
    }

    public static void RemoveCharacterMob(Session otherSession, uint id) {
        Console.WriteLine($"Tentando remover {otherSession.ActiveCharacter.Name}");
    }
}