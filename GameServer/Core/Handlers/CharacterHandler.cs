using GameServer.Core.Base;
using GameServer.Data.Repositories;
using GameServer.Model.Account;
using GameServer.Network;
using GameServer.Service;
using System.Text;

namespace GameServer.Core.Handlers;
public static class CharacterHandler {
    private static async Task SendToCharactersList(Session session, AccountEntitie account) {
        account.Characters = await CharacterRepository.GetCharactersByAccountIdAsync(account.Id);

        var packet = PacketFactory.CreateHeader(0x901);

        packet.Write((uint)account.Id); // AccountID
        packet.Write((uint)0); // Campo desconhecido (Unk)
        packet.Write((uint)0); // Campo não utilizado (NotUse)

        for(ushort i = 0; i < 3; i++) {
            var character = i < account.Characters.Count ? account.Characters.ToList()[i] : null;

            // Nome
            packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0') ?? new string('\0', 16)));

            packet.Write((ushort)(account?.Nation ?? 0)); // Nação
            packet.Write((ushort)(character?.ClassInfo ?? 0)); // Classe

            packet.Write((byte)(character?.Height ?? 7)); // Altura
            packet.Write((byte)(character?.Trunk ?? 119)); // Tronco
            packet.Write((byte)(character?.Leg ?? 119)); // Perna
            packet.Write((byte)(character?.Body ?? 119)); // Corpo

            CharacterService.SetCharEquipsOrdered(character, packet);

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

            packet.Write(new byte[4]); // Null

            // TO-DO: LIDAR COM PERSONAGEM APAGANDO
            if(character != null)
                packet.Write((uint)character.Deleted); // Deleted?
            else {
                packet.Write((uint)0);
            }

            packet.Write((byte)(character?.NumericErrors ?? 0)); // Numeric Erros

            if(!string.IsNullOrEmpty(character?.NumericToken)) packet.Write(true); // Numeric Registered?
            else {
                packet.Write(false);
            }

            packet.Write(new byte[6]); // NotUse (Possivelmente Delet Remain Time?!)

            if(!string.IsNullOrEmpty(character?.Name)) // Assume que o personagem existe se tiver nome (Melhorar?!)
                Console.WriteLine($"Character: {character.Name} -> Carregado");
        }

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    private static void Teleport(Session session, uint positionX, uint positionY) {
        var packet = PacketFactory.CreateHeader(0x301, (ushort)session.ActiveCharacter.OwnerAccountId);

        packet.Write((float)positionX);
        packet.Write((float)positionY);

        for(ushort i = 0; i < 6; i++)
            packet.Write((byte)0);

        packet.Write((byte)1); // Move Type
        packet.Write((byte)0); // Speed 
        packet.Write((uint)0); // Unk_0

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine($"OK -> Teleport x: {positionX} y: {positionY}");
    }

    private static void SendCurrentHpMp(Session session) {
        var packet = PacketFactory.CreateHeader(0x103, (ushort)session.ActiveAccount.Id);


        packet.Write((uint)session.ActiveCharacter.CurrentHealth);
        packet.Write((uint)session.ActiveCharacter.CurrentHealth);
        //packet.Write((uint)session.ActiveCharacter.MaxHealth);
        packet.Write((uint)session.ActiveCharacter.CurrentMana);
        packet.Write((uint)session.ActiveCharacter.CurrentMana);
        //packet.Write((uint)session.ActiveCharacter.MaxMana);
        packet.Write((uint)0); // Null

        PacketFactory.FinalizePacket(packet);

        byte[] packetBytes = packet.GetBytes();
        EncDec.Encrypt(ref packetBytes, packetBytes.Length);

        session.SendPacket(packetBytes);

        PacketPool.Return(packet);
    }

    // TO-DO: Mapear Personagem (Item, Buffs...)
    // Tratar erro de numericas
    public static async Task SendToWorld(Session session, StreamHandler stream) {
        var characterSlot = stream.ReadBytes(4)[0];
        var numericRequestChange = stream.ReadBytes(4)[0];
        var numeric1 = Encoding.ASCII.GetString(stream.ReadBytes(4));
        var numeric2 = Encoding.ASCII.GetString(stream.ReadBytes(4));

        var account = session.ActiveAccount;

        var character = account.Characters[characterSlot];
        if(character == null)
            return;

        session.ActiveCharacter = account.Characters[characterSlot];

        if(numericRequestChange == 0 && string.IsNullOrEmpty(character?.NumericToken)) {
            character.NumericToken = numeric1;
            character.NumericErrors = 0;
            var numericSaved = CharacterRepository.SaveCharacterNumericAsync(character.Name, numeric1, character.NumericErrors).Result;

            if(!numericSaved)
                Console.WriteLine($"{character.Name} -> Erro ao registrar numérica!");

            Console.WriteLine($"{character.Name} -> Numérica Registrada!");
        }
        else if(numericRequestChange == 1 && character.NumericToken == numeric1) {
            // TO-DO: Mapear e enviar para o mundo
            Console.WriteLine($"{character.Name} -> Numérica Correta!");
        }
        else if(numericRequestChange == 2 && character.NumericToken == numeric2) {
            character.NumericToken = numeric1;
            character.NumericErrors = 0;
            var numericaSaved = CharacterRepository.SaveCharacterNumericAsync(character.Name, numeric1, character.NumericErrors).Result;

            if(!numericaSaved)
                Console.WriteLine($"{character.Name} -> Erro ao trocar numérica!");

            Console.WriteLine($"{character.Name} -> Numérica Alterada!");
        }
        else {
            Console.WriteLine($"{character.Name} -> Numérica errada!");
            await SendToCharactersList(session, account);
            GameMessage(session, 16, 0, "Numerica incorreta");
            character.NumericErrors += 1;
            return;
        }

        // QUE MERDA É ESSA E PRA QUE SERVE?
        //SendData(session, 0xCCCC, 0x1);
        //SendData(session, 0x186, 0x1);
        //SendData(session, 0x186, 0x1);
        //SendData(session, 0x186, 0x1);

        var packet = PacketFactory.CreateHeader(0x925, 0x7535);

        // Serial
        packet.Write((uint)account.Id);
        // TO-DO: TFirst Login 
        packet.Write((uint)1);
        //packet.Write((uint)character.FirstLogin);

        packet.Write(account.ConnectionId); // CharacterId
        packet.Write(character.Id); // CharacterId

        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0'))); // Name

        packet.Write((byte)account.Nation); // Nation
        packet.Write(character.ClassInfo); // Classe

        packet.Write((byte)0); // Null_0

        packet.Write((ushort)character.Strength); // Strength
        packet.Write((ushort)character.Agility); // Agility
        packet.Write((ushort)character.Intelligence); // Intelligence
        packet.Write((ushort)character.Constitution); // Constitution
        packet.Write((ushort)character.Luck); // Luck
        packet.Write((ushort)character.Status); // Status

        packet.Write(character.Height); // Height
        packet.Write(character.Trunk); // Trunk
        packet.Write(character.Leg); // Leg
        packet.Write(character.Body); // Body

        packet.Write(character.CurrentHealth); // Current HP
        packet.Write(character.MaxHealth); // Max Hp
        packet.Write(character.CurrentMana); // Current Mana
        packet.Write(character.MaxMana); // Max Mana

        packet.Write((uint)0); // TO-DO: Server reset time

        packet.Write(character.Honor); // Honor
        packet.Write(character.KillPoint); // Pvp
        packet.Write(character.Infamia); // Infamia

        packet.Write((ushort)0); // TO-DO: Evil Points
        packet.Write((ushort)0); // TO-DO: Skill Points

        packet.Write((ushort)0); // Unk_0

        for(ushort i = 0; i < 60; i++)
            packet.Write((byte)0); // Null_1

        packet.Write((ushort)0); // Unk_1

        packet.Write((ushort)character.PhysicDamage); // Physic Damage
        packet.Write((ushort)character.PhysicDefense); // Physic Defense
        packet.Write((ushort)character.MagicDamage); // Magic Damage
        packet.Write((ushort)character.MagicDefense); // Magic Defense
        packet.Write((ushort)character.BonusDamage); // Bonus Damage

        for(ushort i = 0; i < 10; i++)
            packet.Write((byte)0); // Null_2

        // Calculados dinamicamente?!
        packet.Write((ushort)0); // Critical
        packet.Write((byte)0); // Miss
        packet.Write((byte)0); // Accuracy

        packet.Write((ushort)0);     // Null_3

        packet.Write((long)character.Experience);    // Exp
        packet.Write((ushort)character.Level);     // Level
        packet.Write((ushort)0);     // GuildIndex/Logo ??

        for(ushort i = 0; i < 32; i++)
            packet.Write((byte)0); // Null_4

        for(ushort i = 0; i < 20; i++)
            packet.Write((ushort)0); // BuffsId

        for(ushort i = 0; i < 20; i++)
            packet.Write((uint)0);   // BuffsDuration

        var orderedEquips = CharacterService.GetCharEquipsOrdered(character.Equips);

        foreach(var equip in orderedEquips) {
            packet.Write((byte)equip.Value.ItemId);
        }

        packet.Write((uint)0);  // Null_5

        var orderedItens = CharacterService.GetCharInventoryOrdered(character.Inventory);

        foreach(var equip in orderedEquips) {
            packet.Write((byte)equip.Value.ItemId);
        }

        packet.Write((long)character.Gold);  // Gold

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

        //Pran
        packet.Write(Encoding.ASCII.GetBytes("Pran 1".PadRight(16, '\0')));
        packet.Write(Encoding.ASCII.GetBytes("Pran 2".PadRight(16, '\0')));

        packet.Write((uint)0); // Unk_12

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        Console.WriteLine("Packet Data: {0}", BitConverter.ToString(packetData));

        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        session.ActiveCharacter = character;

        Console.WriteLine($"OK -> HandleSendToWorld");

        Teleport(session, character.PositionX, character.PositionY);
    }

    private static void SendData(Session session, ushort packetCode, uint data) {
        var account = session.ActiveAccount;

        var packet = PacketFactory.CreateHeader(0x1, (ushort)account.Id);
        packet.Write(data);

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine($"OK -> SendData ({packetCode}) {data}");
    }

    //TO-DO: Enviar atributos do mundo para o personagem
    public static void SendToWorldSends(Session session) {
        if(session.ActiveCharacter == null) {
            GameMessage(session, 16, 0, "Erro ao carregar personagem");
            return;
        }

        CreateCharacterMob(session);
        SpawnCharacter(session);
        SendCurrentLevel(session);
        SendStatus(session);
        SendCurrentHpMp(session);
        SendAttributes(session);

        ItemHandler.UpdateEquips(session, true);
        ItemHandler.UpdateInventory(session, true);

        // Enviar informações de guilda, se o jogador tiver
        //if(player.GuildIndex > 0) {
        //    SendGuildInfo(session, player);
        //    SendGuildPlayers(session, player.GuildIndex);
        //}

        // Enviar habilidades do jogador
        //SendPlayerSkills(session, player);

        // Configuração inicial de EXP (se for 0, ajusta para 1)
        //if(player.Experience == 0) {
        //RemoveItemIfExists(session, 5284);
        //    player.Experience = 1;
        //    SendRefreshLevel(session, player);
        //}

        // Atualiza inventário de cash
        //SendCashInventory(session, player);

        // Verifica tempo premium
        //if(player.PremiumTime > DateTime.Now) {
        //    SendAccountStatus(session, player);
        //}
        //else {
        //    SendClientMessage(session, 16, 0, "Seu auxílio poderoso expirou.");
        //}

        // Verifica se tem item premium no inventário (8250)
        //foreach(var item in player.Inventory) {
        //    if(item.Index == 8250 && player.PremiumTime <= DateTime.Now) {
        //        SendClientMessage(session, 16, 0, "Seu auxílio poderoso foi ativado. Você tem 30 dias Premium.");
        //        player.PremiumTime = DateTime.Now.AddDays(30);
        //        SendAccountStatus(session, player);
        //    }
        //}

        // Enviar buffs ativos
        //foreach(var buff in player.Buffs) {
        //    if(buff.Index > 0) {
        //        AddBuff(session, buff);
        //    }
        //}

        // Atualizações finais de status
        //SendRefreshBuffs(session, player);
        //SendQuests(session, player);
        //SendNationInformation(session, player);
        //SendReliquesToPlayer(session, player);
        //SendStatus(session, player);

        // Se o jogador tiver montaria equipada, envia atualização
        //if(player.Equip[9].Index > 0) {
        //    SendRefreshItemSlot(session, EquipType.Mount, player.Equip[9]);
        //}

        Console.WriteLine($"OK -> SendToWorldSends");
    }

    private static void SendAttributes(Session session) {
        var character = session.ActiveCharacter;
        var account = session.ActiveAccount;

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

        var packet = PacketFactory.CreateHeader(0x108, (ushort)account.Id);

        packet.Write((ushort)character.Level);
        packet.Write((ushort)0xCC); // Unk
        packet.Write((long)character.Experience); // Exp

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

        packet.Write((ushort)100); // Dano Fisico
        packet.Write((ushort)100); // Def Fisica
        packet.Write((ushort)100); // Dano Magico
        packet.Write((ushort)100); // Def Magica

        packet.Write(new byte[6]); // Null_0

        packet.Write((ushort)60); // Speed Move

        packet.Write((ushort)0); // Atack Ability?

        packet.Write(new byte[6]); // Null_1

        packet.Write((ushort)20); // Critical
        packet.Write((byte)20); // Esquiva
        packet.Write((byte)20); // Acerto
        packet.Write((ushort)20); // Double
        packet.Write((ushort)20); // Resist

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine("OK -> SendStatus");
    }

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
        uint time = stream.ReadUInt32();
        byte[] macAddr = stream.ReadBytes(14);
        ushort version = stream.ReadUInt16();
        uint null_0 = stream.ReadUInt32();
        string token = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');

        Console.WriteLine($"ConnectionId: {connectionId}");
        Console.WriteLine($"Username: {username}");
        Console.WriteLine($"Time: {time}");
        Console.WriteLine($"MacAddr: {BitConverter.ToString(macAddr)}");
        Console.WriteLine($"Version: {version}");
        Console.WriteLine($"Null_0: {null_0}");
        Console.WriteLine($"Token: {token}");

        //if(version != 290 || version == 290) {
        //    GameMessage(session, 16, 0, "Versao errada mane");
        //    return;
        //}

        var account = await AccountRepository.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        account.ConnectionId = connectionId;

        await SendToCharactersList(session, account);

        Console.WriteLine($"OK -> ChararactersList");

        session.ActiveAccount = account;
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

        account.Characters = await CharacterRepository.GetCharactersByAccountIdAsync(account.Id);

        Console.WriteLine($"Personagem criado: {character.Name}");

        await SendToCharactersList(session, account);
    }

    public static async Task ChangeChar(Session session) {
        var account = session.ActiveAccount;

        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        await SendToCharactersList(session, account);
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
    public static void UpdateRotation(StreamHandler stream, Session session) {
        var rotation = BitConverter.ToUInt16(stream.ReadBytes(4));

        Console.WriteLine($"Rotation: {rotation}");

        if(session.ActiveCharacter.Rotation != rotation)
            session.ActiveCharacter.Rotation = rotation;
    }

    private static void CreateCharacterMob(Session session) {
        if(session == null) return;

        var account = session.ActiveAccount;
        var character = session.ActiveCharacter;

        var packet = PacketFactory.CreateHeader(0x349, (ushort)account.Id);

        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0')));

        CharacterService.SetCharEquipsOrdered(character, packet);

        // Item Effect?
        for(ushort j = 0; j < 12; j++) {
            packet.Write((byte)0);
        }

        packet.Write(character.PositionX);
        packet.Write(character.PositionY);

        packet.Write((uint)character.Rotation);

        packet.Write(character.MaxHealth);
        packet.Write(character.MaxMana);
        packet.Write(character.CurrentHealth);
        packet.Write(character.CurrentMana);

        packet.Write((byte)0); // Unk

        packet.Write(character.SpeedMove);

        packet.Write((byte)0); // SpawnType (0: Normal, 1: Teleporte, 2: BabyGen)

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
        for(ushort i = 0; i < 4; i++) {
            packet.Write((ushort)0);
        }

        packet.Write((byte)0); // Unk_0
        packet.Write((byte)0); // TO-DO: ChaosPoints

        packet.Write((long)0); // Null_0

        packet.Write((byte)0); // TitleId
        packet.Write((byte)0); // TitleLevel

        packet.Write((ushort)0); // Null_1

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine($"OK -> CreateCharacterMob");
    }

    private static void SpawnCharacter(Session session) {
        var account = session.ActiveAccount;
        var character = session.ActiveCharacter;

        var packet = PacketFactory.CreateHeader(0x35E, (ushort)account.Id);

        var orderedEquips = new Dictionary<int, ushort>();

        for(ushort j = 0; j < 8; j++) {
            orderedEquips[j] = 0;
        }

        foreach(var equip in character?.Equips ?? []) {
            if(equip.Slot >= 0 && equip.Slot < 8) {
                orderedEquips[equip.Slot] = (ushort)equip.ItemId;
            }
        }

        for(ushort j = 0; j < 8; j++) {
            packet.Write(orderedEquips[j]);
        }

        packet.Write(character.PositionX);
        packet.Write(character.PositionY);

        packet.Write((uint)character.Rotation);

        packet.Write(character.MaxHealth);
        packet.Write(character.MaxMana);
        packet.Write(character.CurrentHealth);
        packet.Write(character.CurrentMana);

        packet.Write((ushort)0); // Unk_0

        packet.Write((ushort)character.Level); // Level

        packet.Write((ushort)0); // Null_0

        packet.Write((bool)false); // IsService 

        for(ushort i = 0; i < 4; i++) {
            packet.Write((byte)0); // TO-DO: Effects
        }

        packet.Write((byte)0); // SpawnType

        packet.Write(character.Height);
        packet.Write(character.Trunk);
        packet.Write(character.Leg);

        packet.Write((ushort)character.Body);

        packet.Write((byte)0); // Mob Type

        packet.Write((byte)account.Nation); // Nation

        packet.Write((ushort)0x7535); // Mob Name?!

        for(ushort i = 0; i < 3; i++) {
            packet.Write((ushort)0); // Unk_1
        }

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine("OK -> SpawnCharacter");
    }

    // TO-DO: TRATAR PLAYER MORTO
    internal static void MoveChar(StreamHandler stream, Session session) {
        var positionX = stream.ReadSingle();
        var positionY = stream.ReadSingle();
        var null_0 = stream.ReadBytes(6);
        var moveType = stream.ReadByte();
        var speed = stream.ReadByte();
        var unk = stream.ReadUInt32();

        Console.WriteLine($"PositionX: {positionX}, PositionY: {positionY}, MoveType: {moveType}, Speed: {speed}, Unk: {unk}");

        //var packet = PacketFactory.CreateHeader(0x301);

        //packet.Write(session.ActiveCharacter.PositionX);
        //packet.Write(session.ActiveCharacter.PositionY);

        //packet.Write(new byte[6]); // Null

        //packet.Write((byte)0); // MoveType (0: normal 1: teleport ?!)
        //packet.Write((byte)0); // Speed

        //packet.Write((uint)0); // Unk
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
