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

        for(int i = 0; i < 3; i++) {
            var character = i < account.Characters.Count ? account.Characters.ToList()[i] : null;

            // Nome
            packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0') ?? new string('\0', 16)));

            packet.Write((ushort)(account?.Nation ?? 0)); // Nação
            packet.Write((ushort)(character?.ClassInfo ?? 0)); // Classe

            if(character == null) {
                packet.Write((byte)0); // Altura
                packet.Write((byte)0); // Tronco
                packet.Write((byte)0); // Perna
                packet.Write((byte)0); // Corpo
            }
            else {
                packet.Write((byte)(character?.Height ?? 0)); // Altura
                packet.Write((byte)(character?.Trunk ?? 0)); // Tronco
                packet.Write((byte)(character?.Leg ?? 0)); // Perna
                packet.Write((byte)(character?.Body ?? 0)); // Corpo

                packet.Write((ushort)(character?.Equips?.ElementAtOrDefault(0)?.ItemId ?? 0));
                packet.Write((ushort)(character?.Equips?.ElementAtOrDefault(1)?.ItemId ?? 0));

                // CHEGA NULO
                for(int k = 2; k < 8; k++) {
                    if(character?.Equips != null && character.Equips.Count > k) packet.Write((ushort)character.Equips[k].App);
                    else {
                        packet.Write((ushort)0);
                    }
                }
            }

            for(int k = 0; k < 12; k++) {
                packet.Write((byte)0); //Refine?
            }

            packet.Write((ushort)(character?.Strength ?? 0)); // Str
            packet.Write((ushort)(character?.Agility ?? 0)); // Agi
            packet.Write((ushort)(character?.Intelligence ?? 0)); // Int
            packet.Write((ushort)(character?.Constitution ?? 0)); // Cons
            packet.Write((ushort)(character?.Luck ?? 0)); // Luck
            packet.Write((ushort)(character?.Status ?? 0)); // Status

            packet.Write((ushort)(character?.Level ?? 65534)); // Level

            packet.Write(new byte[6]); // Null

            packet.Write((long)(character?.Experience ?? 1)); // Exp
            packet.Write((long)(character?.Gold ?? 0)); // Gold

            packet.Write(new byte[4]); // Null

            // TO-DO: LIDAR COM PERSONAGEM APAGANDO
            if(character != null) packet.Write((uint)character.Deleted); // Deleted?
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

        for(int i = 0; i < 6; i++)
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

    // TO-DO: Mapear Personagem (Item, Buffs...)
    // Tratar erro de numericas
    public static async Task SendToNation(Session session, StreamHandler stream) {
        var characterSlot = stream.ReadBytes(4)[0];
        var numericRequestChange = stream.ReadBytes(4)[0];
        var numeric1 = Encoding.ASCII.GetString(stream.ReadBytes(4));
        var numeric2 = Encoding.ASCII.GetString(stream.ReadBytes(4));

        var account = session.ActiveAccount;

        var character = account.Characters[characterSlot];

        if(character == null)
            return;

        if(numericRequestChange == 0 && string.IsNullOrEmpty(character?.NumericToken)) {
            character.NumericToken = numeric1;
            character.NumericErrors = 0;
            var numericSaved = CharacterRepository.SaveCharacterNumericAsync(character.Name, numeric1, (int)character.NumericErrors).Result;

            if(!numericSaved)
                Console.WriteLine($"{character.Name} -> Erro ao registrar numérica!");

            Console.WriteLine($"{character.Name} -> Numérica Registrada!");
            character.FirstLogin = 0;
        }
        else if(numericRequestChange == 1 && character.NumericToken == numeric1) {
            // TO-DO: Mapear e enviar para o mundo
            Console.WriteLine($"{character.Name} -> Numérica Correta!");

            if(character.FirstLogin == 0) character.FirstLogin = 1;
        }
        else if(numericRequestChange == 2 && character.NumericToken == numeric2) {
            character.NumericToken = numeric1;
            character.NumericErrors = 0;
            var numericaSaved = CharacterRepository.SaveCharacterNumericAsync(character.Name, numeric1, (int)character.NumericErrors).Result;

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

        var packet = PacketFactory.CreateHeader(0x925, 0x7535);

        // TO-DO:
        packet.Write((uint)1); // AccountSerial

        packet.Write((uint)account.Id); // AccountId
        packet.Write((byte)character.FirstLogin); // First Login
        packet.Write(character.Id); // CharacterId

        packet.Write(Encoding.ASCII.GetBytes(character.Name.PadRight(16, '\0'))); // Name

        packet.Write((byte)account.Nation); // Nation
        packet.Write((byte)character.ClassInfo); // Classe

        packet.Write((ushort)0); // Null_0

        packet.Write((ushort)character.Strength); // Strength
        packet.Write((ushort)character.Agility); // Agility
        packet.Write((ushort)character.Intelligence); // Intelligence
        packet.Write((ushort)character.Constitution); // Constitution
        packet.Write((ushort)character.Luck); // Luck
        packet.Write((ushort)character.Status); // Status

        packet.Write((byte)character.Height); // Height
        packet.Write((byte)character.Trunk); // Trunk
        packet.Write((byte)character.Leg); // Leg
        packet.Write((byte)character.Body); // Body

        packet.Write(character.MaxHealth); // Max HP
        packet.Write(character.CurrentHealth); // Current HP
        packet.Write(character.MaxMana); // Max Mana
        packet.Write(character.CurrentMana); // Current Mana

        packet.Write((uint)DateTime.UtcNow.AddDays(1).Ticks); // TO-DO: Server reset time

        packet.Write(character.Honor); // Honor
        packet.Write(character.KillPoint); // KillPoint
        packet.Write(character.Infamia); // Infamia
        packet.Write((ushort)0); // TO-DO: Evil Points
        packet.Write((ushort)0); // TO-DO: Skill Points

        packet.Write((ushort)0); // Unk_0

        for(int i = 0; i < 60; i++)
            packet.Write((byte)0); // Null_1

        packet.Write((ushort)0); // Unk_1

        packet.Write((ushort)character.PhysicDamage); // Physic Damage
        packet.Write((ushort)character.PhysicDefense); // Physic Defense
        packet.Write((ushort)character.MagicDamage); // Magic Damage
        packet.Write((ushort)character.MagicDefense); // Magic Defense
        packet.Write((ushort)character.BonusDamage); // Bonus Damage

        for(int i = 0; i < 10; i++)
            packet.Write((byte)0); // Null_2

        // Calculados dinamicamente?!
        packet.Write((ushort)0); // Critical
        packet.Write((byte)0); // Miss
        packet.Write((byte)0); // Accuracy

        packet.Write((ushort)0);     // Null_3

        packet.Write((long)character.Experience);    // Exp
        packet.Write((ushort)character.Level);     // Level
        packet.Write((ushort)0);     // GuildIndex

        packet.Write(new byte[32]);  // Null_4

        for(int i = 0; i < 20; i++)
            packet.Write((ushort)0); // BuffsId

        for(int i = 0; i < 20; i++)
            packet.Write((uint)0);   // BuffsDuration

        // Equipamentos (16 slots)
        for(int i = 0; i < 16; i++) {
            packet.Write((ushort)0); // Index
            packet.Write((ushort)0); // App
            packet.Write((long)0); // Identification

            // Item Effect
            for(int j = 0; j < 3; j++) {
                packet.Write((byte)0); // Index
                packet.Write((byte)0); // Value
            }

            packet.Write((byte)0); // Min
            packet.Write((byte)0); // Max
            packet.Write((ushort)0); // Refine
            packet.Write((ushort)0); // Time
        }

        packet.Write((ushort)0);  // Null_5

        // Inventário (64 slots)
        for(int i = 0; i < 64; i++) {
            packet.Write((ushort)0); // Index
            packet.Write((ushort)0); // App
            packet.Write((long)0); // Identification

            // Item Effect
            for(int j = 0; j < 3; j++) {
                packet.Write((byte)0); // Index
                packet.Write((byte)0); // Value
            }

            packet.Write((byte)0); // Min
            packet.Write((byte)0); // Max
            packet.Write((ushort)0); // Refine
            packet.Write((ushort)0); // Time
        }

        packet.Write(character.Gold);  // Gold

        // Unk_2
        for(int i = 0; i < 192; i++) {
            packet.Write((byte)0);
        }

        // Quests
        for(int i = 0; i < 16; i++) {
            packet.Write((ushort)0); // Id

            for(int j = 0; j < 10; j++) {
                packet.Write((byte)0); // Unk (Progress?!)
            }
        }

        // Unk_3
        for(int i = 0; i < 212; i++) {
            packet.Write((byte)0);
        }

        packet.Write((uint)0); // Unk_4
        packet.Write((uint)0); // Location

        // Unk_5
        for(int i = 0; i < 128; i++) {
            packet.Write((byte)0);
        }

        packet.Write((uint)DateTime.UtcNow.AddDays(1).Ticks - DateTime.UtcNow.Ticks); // TO-DO: CreationTime

        for(int i = 0; i < 436; i++) {
            packet.Write((byte)0);
        }

        packet.Write(Encoding.ASCII.GetBytes(character.NumericToken));

        for(int i = 0; i < 212; i++) {
            packet.Write((byte)0);
        }

        // Skill List
        for(int i = 0; i < 60; i++) {
            packet.Write((ushort)0);
        }

        // Item Bar
        for(int i = 0; i < 24; i++) {
            packet.Write((uint)0);
        }

        packet.Write((uint)0); // NULL_6

        // TitleCategoryLevel
        for(int i = 0; i < 12; i++) {
            packet.Write((uint)0);
        }

        // Unk_7
        for(int i = 0; i < 80; i++) {
            packet.Write((byte)0);
        }

        packet.Write((ushort)0); // Active Title

        packet.Write((uint)0); // Null_8

        for(int i = 0; i < 48; i++)
            packet.Write((ushort)0); // TitleProgressType8

        for(int i = 0; i < 2; i++)
            packet.Write((ushort)0); // TitleProgressType9

        packet.Write((ushort)0); // TitleProgressType4
        packet.Write((ushort)0); // TitleProgressType10
        packet.Write((ushort)0); // TitleProgressType7
        packet.Write((ushort)0); // TitleProgressType11
        packet.Write((ushort)0); // TitleProgressType12
        packet.Write((ushort)0); // TitleProgressType13
        packet.Write((ushort)0); // TitleProgressType15
        packet.Write((ushort)0); // TitleProgressUnk

        for(int i = 0; i < 22; i++)
            packet.Write((ushort)0); // TitleProgressType16

        packet.Write((ushort)0); // TitleProgressType23

        for(int i = 0; i < 200; i++)
            packet.Write((ushort)0); // TitleProgress

        packet.Write((uint)DateTime.Now.AddDays(1).Ticks); // EndDayTime

        packet.Write((uint)0); // Null_9
        packet.Write((uint)0); // Unk_10

        // Null_10
        for(int i = 0; i < 52; i++) {
            packet.Write((byte)0);
        }

        packet.Write((uint)DateTime.UtcNow.Ticks); // Utc
        packet.Write((uint)DateTime.Now.Ticks); // LoginTime

        // Unk_11
        for(int i = 0; i < 12; i++) {
            packet.Write((byte)0);
        }

        //Pran
        packet.Write(Encoding.ASCII.GetBytes("Pran 1".PadRight(16, '\0')));
        packet.Write(Encoding.ASCII.GetBytes("Pran 2".PadRight(16, '\0')));

        packet.Write((uint)0); // Unk_12

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        session.ActiveCharacter = character;

        Console.WriteLine($"OK -> HandleSendToWorld");

        Teleport(session, character.PositionX, character.PositionY);
    }

    //TO-DO: Enviar atributos do mundo para o personagem
    public static void SendToWorldSends(Session session) {
        if(session.ActiveCharacter == null) {
            GameMessage(session, 16, 0, "Erro ao carregar personagem");
            return;
        }

        //Enviar criação do personagem no mundo
        //CreateCharacterMob(session, 0);

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

        Console.WriteLine($"OK -> SendToWorldSends!");
    }

    public static async Task SelectedChannel(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');


        var account = await AccountRepository.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        await SendToCharactersList(session, account);

        Console.WriteLine($"OK -> ChararactersList");

        session.ActiveAccount = account;
    }

    public static async Task CreateCharacter(Session session, StreamHandler stream) {
        var characterData = CharacterService.ParseCharacterData(session, stream);
        if(string.IsNullOrWhiteSpace(characterData.Name)) {
            GameMessage(session, 16, 0, "PERSONAGEM INVALIDO");
            return;
        }

        var account = session.ActiveAccount;
        if(account == null || account.Characters.Count >= 3) {
            GameMessage(session, 16, 0, "QUANTIDADE MAXIMA: 3");
            return;
        }

        var success = await CharacterService.CreateCharacterAsync(characterData, account);
        if(!success) {
            GameMessage(session, 16, 0, "ERRO: CRIAR PERSONAGEM");
            return;
        }

        Console.WriteLine($"Personagem criado: {characterData.Name}");

        await SendToCharactersList(session, account);
    }

    public static async Task ChangeCharacter(Session session) {
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
