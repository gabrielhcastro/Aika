using GameServer.Core.Base;
using GameServer.Data.Repositories;
using GameServer.Model.Account;
using GameServer.Model.World;
using GameServer.Network;
using GameServer.Service;
using System.IO;
using System.Text;

namespace GameServer.Core.Handlers;
public static class CharacterHandler {
    private static async Task SendToCharactersList(Session session) {
        var account = session.ActiveAccount;
        session.ActiveAccount.Characters = await CharacterRepository.GetCharactersByAccountIdAsync(account.Id);

        var packet = PacketFactory.CreateHeader(0x901, (ushort)account.Id);

        packet.Write((uint)5077);
        packet.Write((uint)0); // Unk
        packet.Write((uint)0); // Unk

        for(ushort i = 0; i < 3; i++) {
            var character = i < account.Characters.Count ? account.Characters.ToList()[i] : null;

            // Nome
            packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0') ?? new string('\0', 16)));

            packet.Write((ushort)(account?.Nation ?? 0));
            packet.Write((ushort)(character?.ClassInfo ?? 0));

            packet.Write((byte)(character?.Height ?? 0));
            packet.Write((byte)(character?.Trunk ?? 0));
            packet.Write((byte)(character?.Leg ?? 0));
            packet.Write((byte)(character?.Body ?? 0));

            CharacterService.SetCharLobbyOrdered(character, packet);

            for(ushort k = 0; k < 12; k++) {
                packet.Write((byte)0); //Refine?
            }

            packet.Write((ushort)(character?.Strength ?? 0)); // Str
            packet.Write((ushort)(character?.Agility ?? 0)); // Agi
            packet.Write((ushort)(character?.Intelligence ?? 0)); // Int
            packet.Write((ushort)(character?.Constitution ?? 0)); // Cons
            packet.Write((ushort)(character?.Luck ?? 0)); // Luck
            packet.Write((ushort)(character?.Status ?? 0)); // Status

            packet.Write((ushort)(character?.Level ?? 0)); // Level

            packet.Write(new byte[6]); // Null

            packet.Write((long)(character?.Experience ?? 1)); // Exp
            packet.Write((long)(character?.Gold ?? 0)); // Gold

            packet.Write((uint)0); // Null

            packet.Write((uint)0); // > 0 pode excluir

            packet.Write((uint)0); // Erro de numérica (só visual não funciona)


            if(!string.IsNullOrEmpty(character?.Name))
                Console.WriteLine($"Character: {character.Name} -> Carregado");
        }

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    // TO-DO: Mapear Personagem (Item, Buffs...)
    // Tratar erro de numericas
    public static void SendToWorld(Session session, StreamHandler stream) {
        var characterSlot = stream.ReadBytes(4)[0];
        var numericRequestChange = stream.ReadBytes(4)[0];
        var numeric1 = Encoding.ASCII.GetString(stream.ReadBytes(4));
        var numeric2 = Encoding.ASCII.GetString(stream.ReadBytes(4));

        var account = session.ActiveAccount;

        var character = account.Characters[characterSlot];
        if(character == null)
            return;

        session.ActiveCharacter = account.Characters[characterSlot];

        SendData(session, 0xCCCC, 0x1);
        SendData(session, 0x186, 0x1);
        SendData(session, 0x186, 0x1);
        SendData(session, 0x186, 0x1);
        SendClientIndex(session, session.ActiveAccount.Id);

        var packet = PacketFactory.CreateHeader(0x925, 0x7535);

        // Serial
        packet.Write((uint)5077);

        // TO-DO: First Login 
        packet.Write((uint)1);
        //packet.Write((uint)character.FirstLogin);

        packet.Write(account.ConnectionId);
        packet.Write(character.Id);
        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0')));
        packet.Write((byte)account.Nation);
        packet.Write(character.ClassInfo);

        packet.Write(character.Id); // CharacterId

        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0'))); // Name

        packet.Write((byte)account.Nation); // Nation
        packet.Write(character.ClassInfo); // Classe

        packet.Write((byte)0); // Null_0

        packet.Write((ushort)character.Strength);
        packet.Write((ushort)character.Agility);
        packet.Write((ushort)character.Intelligence);
        packet.Write((ushort)character.Constitution);
        packet.Write((ushort)character.Luck);
        packet.Write((ushort)character.Status);
        packet.Write(character.Height);
        packet.Write(character.Trunk);
        packet.Write(character.Leg);
        packet.Write(character.Body);
        packet.Write(character.MaxHealth);
        packet.Write(character.CurrentHealth);
        packet.Write(character.MaxMana);
        packet.Write(character.CurrentMana);

        packet.Write((uint)0); // TO-DO: Server reset time

        packet.Write(character.Honor);
        packet.Write(character.KillPoint);
        packet.Write(character.Infamia);

        packet.Write((ushort)0); // TO-DO: Evil Points
        packet.Write((ushort)0); // TO-DO: Skill Points

        packet.Write((ushort)0); // Unk_0

        for(ushort i = 0; i < 60; i++)
            packet.Write((byte)0); // Null_1

        packet.Write((ushort)0); // Unk_1

        packet.Write((ushort)character.PhysicDamage);
        packet.Write((ushort)character.PhysicDefense);
        packet.Write((ushort)character.MagicDamage);
        packet.Write((ushort)character.MagicDefense);
        packet.Write((ushort)character.BonusDamage);

        for(ushort i = 0; i < 10; i++)
            packet.Write((byte)0); // Null_2

        // Calculados dinamicamente?!
        packet.Write((ushort)0); // Critical
        packet.Write((byte)0); // Miss
        packet.Write((byte)0); // Accuracy
        packet.Write((ushort)0);     // Null_3

        packet.Write((long)character.Experience);
        packet.Write((ushort)character.Level);

        packet.Write((ushort)0);     // GuildIndex/Logo ??

        for(ushort i = 0; i < 32; i++)
            packet.Write((byte)0); // Null_4

        for(ushort i = 0; i < 20; i++)
            packet.Write((ushort)0); // BuffsId

        for(ushort i = 0; i < 20; i++)
            packet.Write((uint)0);   // BuffsDuration

        CharacterService.SetCharEquipsOrdered(character, stream);

        packet.Write((uint)0);  // Null_5

        CharacterService.SetCharInventoryOrdered(character, stream);

        packet.Write((long)character.Gold);

        // Unk_2
        for(ushort i = 0; i < 192; i++) {
            packet.Write((byte)0);
        }

        // Quests
        for(ushort i = 0; i < 16; i++) {
            packet.Write((ushort)0); // Id

            for(ushort j = 0; j < 10; j++) {
                packet.Write((byte)0); // Unk (Progress?!)
            }
        }

        // Unk_3
        for(ushort i = 0; i < 212; i++) {
            packet.Write((byte)0);
        }

        packet.Write((uint)0); // Unk_4
        packet.Write((ushort)0); // Location

        // Unk_5
        for(ushort i = 0; i < 128; i++) {
            packet.Write((byte)0);
        }

        packet.Write((uint)DateTime.Parse(character.CreationTime).Ticks);

        for(ushort i = 0; i < 436; i++) {
            packet.Write((byte)0);
        }

        packet.Write(Encoding.ASCII.GetBytes(character.NumericToken));

        for(ushort i = 0; i < 212; i++) {
            packet.Write((byte)0);
        }

        // Skill List
        for(ushort i = 0; i < 60; i++) {
            packet.Write((ushort)0);
        }

        // Item Bar
        for(ushort i = 0; i < 24; i++) {
            packet.Write((uint)0);
        }

        packet.Write((uint)0); // NULL_6

        // TitleCategoryLevel
        for(ushort i = 0; i < 12; i++) {
            packet.Write((uint)0);
        }

        // Unk_7
        for(ushort i = 0; i < 80; i++) {
            packet.Write((byte)0);
        }

        packet.Write((ushort)0); // Active Title

        packet.Write((uint)0); // Null_8

        for(ushort i = 0; i < 48; i++)
            packet.Write((ushort)0); // TitleProgressType8

        for(ushort i = 0; i < 2; i++)
            packet.Write((ushort)0); // TitleProgressType9

        packet.Write((ushort)0); // TitleProgressType4
        packet.Write((ushort)0); // TitleProgressType10
        packet.Write((ushort)0); // TitleProgressType7
        packet.Write((ushort)0); // TitleProgressType11
        packet.Write((ushort)0); // TitleProgressType12
        packet.Write((ushort)0); // TitleProgressType13
        packet.Write((ushort)0); // TitleProgressType15
        packet.Write((ushort)0); // TitleProgressUnk

        for(ushort i = 0; i < 22; i++)
            packet.Write((ushort)0); // TitleProgressType16

        packet.Write((ushort)0); // TitleProgressType23

        for(ushort i = 0; i < 120; i++)
            packet.Write((ushort)0); // TitleProgress

        packet.Write((uint)DateTime.Now.AddDays(1).Ticks); // EndDayTime

        packet.Write((uint)0); // Null_9
        packet.Write((uint)0); // Unk_10

        // Null_10
        for(ushort i = 0; i < 52; i++) {
            packet.Write((byte)0);
        }

        packet.Write((uint)0); // Utc
        packet.Write((uint)DateTime.Now.Ticks); // LoginTime

        // Unk_11
        for(ushort i = 0; i < 12; i++) {
            packet.Write((byte)0);
        }

        //Pran Name
        packet.Write(Encoding.ASCII.GetBytes("Pran 1".PadRight(16, '\0')));
        packet.Write(Encoding.ASCII.GetBytes("Pran 2".PadRight(16, '\0')));

        packet.Write((uint)0); // Unk_12

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();

        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        session.ActiveCharacter = character;

        SessionHandler.AddCharacter(character);

        Console.WriteLine($"OK -> HandleSendToWorld");
    }

    public static void SendToWorldSends(Session session) {
        if(session.ActiveCharacter == null) {
            GameMessage(session, 16, 0, "Erro ao carregar personagem");
            return;
        }

        session.ActiveCharacter.Neighbors = [];
        session.ActiveCharacter.VisiblePlayers = [];
        session.ActiveCharacter.VisibleMobs = [];
        session.ActiveCharacter.VisibleNpcs = [];


        CreateCharacterMob(session, 0);

        CharacterService.SetCurrentNeighbors(session.ActiveCharacter);
        SessionHandler.Instance.UpdateVisibleList(session);

        SendCurrentLevel(session);
        SendStatus(session);
        //SendCurrentHpMp(session);
        SendAttributes(session);
        //Teleport(session, session.ActiveCharacter.PositionX, session.ActiveCharacter.PositionY);

        foreach(var equip in session.ActiveCharacter.Equips) {
            ItemHandler.UpdateItemBySlotAndType(session, equip, true);
        }

        foreach(var item in session.ActiveCharacter.Inventory) {
            ItemHandler.UpdateItemBySlotAndType(session, item, true);
        }

        Console.WriteLine($"OK -> SendToWorldSends");
    }

    public static void Teleport(Session session, Single positionX, Single positionY) {
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

        Console.WriteLine($"OK -> Teleport x: {positionX} y: {positionY}");
    }

    private static void SendData(Session session, ushort packetCode, uint data) {
        var account = session.ActiveAccount;

        var packet = PacketFactory.CreateHeader(packetCode, 0x7535);
        packet.Write(data);

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine($"OK -> SendData ({packetCode}) {data}");
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
        var account = session.ActiveCharacter;
        var packet = PacketFactory.CreateHeader(0x103, (ushort)account.Id);

        packet.Write(session.ActiveCharacter.CurrentHealth);
        packet.Write(session.ActiveCharacter.MaxHealth);
        packet.Write(session.ActiveCharacter.CurrentMana);
        packet.Write(session.ActiveCharacter.MaxMana);

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

        packet.Write((ushort)character.Strength);
        packet.Write((ushort)character.Agility);
        packet.Write((ushort)character.Intelligence);
        packet.Write((ushort)character.Constitution);
        packet.Write((ushort)character.Luck);
        packet.Write((ushort)character.Status);
        packet.Write((ushort)20); // Status Point
        packet.Write((ushort)20); // Skill Point

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine("OK -> SendCurrentLevel");
    }

    private static void SendCurrentLevel(Session session) {
        var character = session.ActiveCharacter;
        var account = session.ActiveAccount;

        var packet = PacketFactory.CreateHeader(0x108, 0x7535);

        packet.Write((ushort)0); // Level
        packet.Write((byte)0xCC); // Unk
        packet.Write((byte)0); // Unk
        packet.Write((uint)0); // XP
        packet.Write(new byte[9]); // Unk

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine("OK -> SendCurrentLevel");
    }

    // TO-DO: CALCULAR STATUS
    private static void SendStatus(Session session) {
        var packet = PacketFactory.CreateHeader(0x10A, 0x7535);

        packet.Write((ushort)60); // Physic Damage
        packet.Write((ushort)31); // Physic Defense
        packet.Write((ushort)0); // Magic Damage
        packet.Write((ushort)31); // Magic Defense
        packet.Write((ushort)20); // Status

        packet.Write(new byte[10]); // Unk

        packet.Write((ushort)40); // Speed Move

        packet.Write((ulong)0); // Unk

        packet.Write((ushort)34); // Critical

        packet.Write((ushort)0); // Unk

        packet.Write((ushort)36); // Miss

        packet.Write((ushort)37); // Accuracy
        packet.Write((ushort)38); // Double
        packet.Write((ushort)39); // Resistence

        packet.Write((ushort)0); // Unk

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine("OK -> SendStatus");
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

        account.ConnectionId = connectionId;
        session.ActiveAccount = account;

        await SendToCharactersList(session);

        Console.WriteLine($"OK -> ChararactersList");
    }

    public static async Task CreateChar(Session session, StreamHandler stream) {
        var character = CharacterService.GenerateInitCharacter(session, stream);
        if(string.IsNullOrWhiteSpace(character.Name)) {
            GameMessage(session, 16, 0, "PERSONAGEM INVALIDO");
            return;
        }

        var account = session.ActiveAccount;
        if(account == null || account.Characters.Count >= 3) {
            GameMessage(session, 16, 0, "QUANTIDADE MAXIMA: 3");
            return;
        }

        var success = await CharacterService.CreateCharAsync(character, account);
        if(!success) {
            GameMessage(session, 16, 0, "ERRO: CRIAR PERSONAGEM");
            return;
        }

        character.Inventory = [];
        account.Characters.Add(character);

        Console.WriteLine($"Personagem criado: {character.Name}");

        await SendToCharactersList(session);
    }

    public static async Task ChangeChar(Session session) {
        var account = session.ActiveAccount;

        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

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

        //UpdateRotationToAll(session, stream);
    }

    public static void CreateCharacterMob(Session session, ushort spawnType, bool sendSelf = true) {
        if(session == null) return;

        var character = session.ActiveCharacter;

        var packet = PacketFactory.CreateHeader(0x349, 1);

        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0')));

        CharacterService.SetCharLobbyOrdered(character, packet);

        // Item Effect?
        packet.Write(new byte[12]);

        packet.Write((Single)character.Position.X);
        packet.Write((Single)character.Position.Y);

        packet.Write((uint)character.Rotation);

        packet.Write(character.CurrentHealth);
        packet.Write(character.MaxHealth);
        packet.Write(character.CurrentMana);
        packet.Write(character.MaxMana);

        packet.Write((byte)0x0A); // Unk

        packet.Write(character.SpeedMove);

        packet.Write((byte)spawnType); // SpawnType (0: Normal, 1: Teleporte, 2: BabyGen)

        packet.Write(character.Height);
        packet.Write(character.Trunk);
        packet.Write(character.Leg);
        packet.Write(character.Body);

        packet.Write((byte)0); // IsService (0 Player, 1 Npc?!)

        packet.Write((ushort)0); // TO-DO: EffectType
        packet.Write((ushort)0); // TO-DO: SetBuffs

        for(ushort i = 0; i < 60; i++) {
            packet.Write((ushort)0); // TO-DO: Buffs
        }

        for(ushort i = 0; i < 60; i++) {
            packet.Write((uint)0); // TO-DO: BuffTime
        }

        packet.Write(Encoding.ASCII.GetBytes(new string('\0', 32))); // Title

        // TO-DO: Guild Index and Nation Index
        //packet.Write((ushort)account.Nation * 4096); 
        packet.Write((ushort)0);

        // Effects?
        packet.Write((ushort)0);
        packet.Write((ushort)0);
        packet.Write((ushort)0);
        packet.Write((ushort)0);

        packet.Write((byte)0); // Unk_0
        packet.Write((byte)0); // TO-DO: ChaosPoints

        packet.Write((uint)0); // Null_0

        packet.Write((byte)0); // TitleId
        packet.Write((byte)0); // TitleLevel

        packet.Write((ushort)0); // Null_1

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);
        SendToVisibles(session, packetData);

        PacketPool.Return(packet);
    }

    public static void SpawnCharacter(Session session, ushort spawnType) {
        var account = session.ActiveAccount;
        var character = session.ActiveCharacter;

        var packet = PacketFactory.CreateHeader(0x35E, (ushort)account.Id);

        CharacterService.SetCharLobbyOrdered(character, packet);

        packet.Write(character.Position.X);
        packet.Write(character.Position.Y);

        packet.Write((uint)character.Rotation);

        packet.Write(character.MaxHealth);
        packet.Write(character.CurrentHealth);
        packet.Write(character.MaxMana);
        packet.Write(character.CurrentMana);

        packet.Write((ushort)0); // Unk_0
        packet.Write((ushort)character.Level); // Unk_1 (Level?!)

        packet.Write((ushort)0); // Null_0
        packet.Write(false); // IsService

        for(int i = 0; i < 4; i++) {
            packet.Write((byte)0); // Effects?
        }
        
        packet.Write((byte)0); // Spawn Type

        packet.Write((ushort)character.Level);
        packet.Write((ushort)0); // Null_0

        packet.Write(false);

        packet.Write(new byte[4]); // Effects

        packet.Write(spawnType); // Spawn Type

        packet.Write(character.Height);
        packet.Write(character.Trunk);
        packet.Write(character.Leg);
        packet.Write((ushort)character.Body); // Body

        packet.Write((byte)0); // Mob Type
        packet.Write((byte)0); // Nation

        packet.Write((ushort)0); // Mob Name

        for(int i = 0; i < 4; i++) {
            packet.Write((ushort)0); // Unk_1
        }

        packet.Write((byte)account.Nation); // Nation

        packet.Write((ushort)0); // Mob Name?
        packet.Write((byte)0);

        packet.Write(new byte[3]); // Unk_1

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);
        session.SendPacket(packetData); // 2x?

        PacketPool.Return(packet);

        Console.WriteLine("OK -> SpawnCharacter");
    }

    // TO-DO: TRATAR PLAYER MORTO
    public static void MoveChar(Session session, StreamHandler stream) {
        var positionX = stream.ReadSingle();
        var positionY = stream.ReadSingle();
        var null_0 = stream.ReadBytes(6);
        var moveType = stream.ReadByte();
        var speed = stream.ReadByte();
        var unk = stream.ReadUInt32();

        var packet = PacketFactory.CreateHeader(0x301, (ushort)session.ActiveAccount.Id);

        packet.Write(session.ActiveCharacter.Position.X);
        packet.Write(session.ActiveCharacter.Position.Y);

        packet.Write((byte)0); // Null

        packet.Write((ushort)session.ActiveCharacter.Rotation); // Null

        packet.Write(moveType); // MoveType (0: normal 1: teleport ?!)
        packet.Write(speed); // Speed

        packet.Write((byte)0); // Unk
        packet.Write((byte)0); // Unk
        packet.Write((byte)0); // Unk
        packet.Write((byte)0); // Unk

        PacketFactory.FinalizePacket(packet);

        byte[] packetByte = packet.GetBytes();
        EncDec.Encrypt(ref packetByte, packetByte.Length);

        session.SendPacket(packet);

        PacketPool.Return(packet);
    }

    internal static void UpdateCharInfo(Session session, StreamHandler stream) {
        var index = stream.ReadUInt32();
        var loop = stream.ReadUInt32();

        //var packet = PacketFactory.CreateHeader(0x306, (ushort)index);
        var packet = PacketFactory.CreateHeader(0x306, 1);
        packet.Write(index);
        packet.Write(loop);

        PacketFactory.FinalizePacket(packet);

        var packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine("OK -> UpdateCharInfo");
    }

    public static void SendToVisibles(Session session, byte[] packet) {
        foreach(var otherPlayer in session.ActiveCharacter.VisiblePlayers) {

            var otherPlayerSession = SessionHandler.Instance.GetSessionByCharacterId((ushort)otherPlayer.Id);

            if(session.ActiveCharacter.VisiblePlayers.Any(c => c.Id == otherPlayerSession.ActiveCharacter.Id)) continue;

            if(otherPlayerSession.ActiveCharacter == null)
                continue;

            otherPlayerSession.SendPacket(packet);
        }
    }

    //private static void SetInitialBullets(Player player, int slotIndex, int classCategory) {
    //    if(classCategory == 2) {
    //        SetBullet(player, slotIndex, 4615);
    //    }
    //    else if(classCategory == 3) {
    //        SetBullet(player, slotIndex, 4600);
    //    }
    //}

    //private static void SetBullet(Player player, int slotIndex, int bulletId) {
    //    player.Account.Characters[slotIndex].Base.Equip[15].Index = bulletId;
    //    player.Account.Characters[slotIndex].Base.Equip[15].APP = bulletId;
    //    player.Account.Characters[slotIndex].Base.Equip[15].Refi = 1000;
    //    player.Account.Characters[slotIndex].Base.Inventory[5].Index = bulletId;
    //    player.Account.Characters[slotIndex].Base.Inventory[5].APP = bulletId;
    //    player.Account.Characters[slotIndex].Base.Inventory[5].Refi = 1000;
    //    player.Account.Characters[slotIndex].Base.Inventory[6].Index = bulletId;
    //    player.Account.Characters[slotIndex].Base.Inventory[6].APP = bulletId;
    //    player.Account.Characters[slotIndex].Base.Inventory[6].Refi = 1000;
    //}
}
