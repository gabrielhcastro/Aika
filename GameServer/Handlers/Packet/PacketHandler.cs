using GameServer.Core.Base;
using Shared.Handlers;
using Shared.Models.Account;
using Shared.Models.Character;
using Shared.Models.Item;
using System.Text;
using System.Text.RegularExpressions;

namespace GameServer.Handlers.Packet;
public static class PacketHandler {
    public static async Task HandlePacket(Session session, StreamHandler stream) {
        stream.ReadInt32();
        var sender = stream.ReadUInt16();
        var opcode = stream.ReadUInt16();
        stream.ReadInt32();

        Console.WriteLine($"Opcode received: {opcode}, Sender: {sender}");

        switch(opcode) {
            case 0x81:
            await HandleLogin(session, stream);
            break;
            case 0x685:
            await HandleLoginCharactersList(session, stream);
            break;
            case 0x3E04:
            await HandleCreateCharacter(session, stream);
            break;
            case 0x39D:
            break;
            case 0xF02:
            HandleSendToWorld(session, stream);
            break;
            case 0xF0B:
            SendToWorldSends(session);
            break;
            case 0x668:
            await ChangeCharacterRequest(session);
            break;
            case 0x305:
            UpdateRotation(stream, session);
            break;
            default:
            Console.WriteLine($"Unknown opcode: {opcode}, Sender: {sender}");
            Console.WriteLine("Packet Data: {0} -> {1}", stream.Count, BitConverter.ToString(stream));
            break;
        }
    }

    private static async Task HandleLogin(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');

        var account = await DatabaseHandler.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Login falhou: Conta não encontrada.");
            return;
        }

        var packet = PacketFactory.CreateHeader(0x82);

        packet.Write((uint)account.Id); // AccountID
        packet.Write((uint)session.LastActivity.Ticks); // LoginTime
        packet.Write((ushort)account.Nation); // Nação
        packet.Write((uint)0); // Null_1 (padding)

        packet.Buffer[0] = 0x19; // size 25 bytes fixo login porta 8831
        packet.Buffer[1] = 0x00;

        byte[] packetData = packet.GetBytes();
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine($"Login -> {username} : {DateTime.UtcNow}");
    }

    private static async Task HandleLoginCharactersList(Session session, StreamHandler stream) {
        string username = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0');
        _ = Encoding.ASCII.GetString(stream.ReadBytes(32)).TrimEnd('\0'); // Password

        var account = await DatabaseHandler.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        account.Characters = [];

        SendToCharactersList(session, account);

        Console.WriteLine($"OK -> ChararactersList");

        session.ActiveAccount = account;
    }

    private static async Task HandleCreateCharacter(Session session, StreamHandler stream) {
        CharacterEntitie character = new() {
            OwnerAccountId = BitConverter.ToUInt32(stream.ReadBytes(4), 0),
            Itens = Enumerable.Range(0, 60).Select(_ => new ItemEntitie()).ToList(),
            Equips = Enumerable.Range(0, 8).Select(_ => new ItemEntitie()).ToList()
        };

        var account = session.ActiveAccount;
        if(account == null) {
            Console.WriteLine("Conta não encontrada");
            return;
        }

        var slot = BitConverter.ToUInt32(stream.ReadBytes(4), 0);
        if(slot > 2) {
            SendClientMessage(session, 16, 0, "SLOT_ERROR");
            return;
        }

        if(account?.Characters?.Count == 3) {
            SendClientMessage(session, 16, 0, "Quantidade maxima de personagens");
            return;
        }

        character.Slot = slot;

        var name = Encoding.ASCII.GetString(stream.ReadBytes(16)).TrimEnd('\0');
        if(name.Length > 14) {
            SendClientMessage(session, 16, 0, "Limite de 14 caracteres");
            return;
        }

        var pattern = @"^[A-Za-z][A-Za-z0-9]*$";
        if(!Regex.IsMatch(name, pattern)) {
            SendClientMessage(session, 16, 0, "Somente caracters alfanumericos");
            return;
        }

        var nameExists = DatabaseHandler.VerifyIfCharacterNameExistsAsync(name).Result;

        if(nameExists) {
            SendClientMessage(session, 16, 0, "Nome em uso");
            return;
        }

        character.Name = name;

        var classValue = stream.ReadBytes(2)[0];

        if(classValue < 10 && classValue > 69)
            SendClientMessage(session, 16, 0, "Classe fora dos limites");

        ushort classInfo = GetClassInfo(classValue);

        if(InitialCharacterStatus.TryGetValue(classInfo, out var classConfig)) {
            ApplyInitialCharacterStatus(character, classConfig);
        }
        else {
            Console.WriteLine("Classe inválida.");
            return;
        }

        var hair = BitConverter.ToUInt16(stream.ReadBytes(2), 0);
        if(hair < 7700 || hair > 7731) // Proteção Criar Itens
            SendClientMessage(session, 16, 0, "Cabelo fora dos limites");

        ItemEntitie itemClassInfo = new() {
            Slot = 0,
            SlotType = 0,
        };

        switch(classInfo) {
            case 0:
            itemClassInfo.ItemId = 10;
            character.ClassInfo = 1;
            break;
            case 1:
            itemClassInfo.ItemId = 20;
            character.ClassInfo = 11;
            break;
            case 2:
            itemClassInfo.ItemId = 30;
            character.ClassInfo = 21;
            break;
            case 3:
            itemClassInfo.ItemId = 40;
            character.ClassInfo = 31;
            break;
            case 4:
            itemClassInfo.ItemId = 50;
            character.ClassInfo = 41;
            break;
            case 5:
            itemClassInfo.ItemId = 60;
            character.ClassInfo = 51;
            break;
        }

        ItemEntitie itemHair = new() {
            Slot = 0,
            SlotType = 1,
            ItemId = hair,
        };

        character.Equips[0] = itemClassInfo;
        character.Equips[1] = itemHair;

        character.SpeedMove = 40;

        _ = Encoding.ASCII.GetString(stream.ReadBytes(12)).TrimEnd('\0');

        var local = BitConverter.ToUInt32(stream.ReadBytes(4), 0);
        SetCharacterPosition(character, local);

        bool success = await DatabaseHandler.CreateCharacterAsync(character, account);
        if(!success) {
            SendClientMessage(session, 16, 0, "Erro ao criar personagem");
            return;
        }

        Console.WriteLine($"Personagem criado: {character.Name} no slot {character.Slot}");

        SendToCharactersList(session, account);
    }

    private static readonly Dictionary<ushort, (int Strength, int Intelligence, int Agility, int Constitution, int Luck,
    List<ItemEntitie> Items)> InitialCharacterStatus = new() {
        // Warrior
        [0] = (15, 5, 9, 16, 0, new List<ItemEntitie> {
            new() { Slot = 0, SlotType = 3, ItemId = 1719, App = 1719, MinimalValue = 100, MaxValue = 100 },
            new() { Slot = 0, SlotType = 5, ItemId = 1779, App = 1779, MinimalValue = 100, MaxValue = 100 },
            new() { Slot = 0, SlotType = 6, ItemId = 1069, App = 1069, MinimalValue = 160, MaxValue = 160 }
        }),

        // Templaria
        [1] = (14, 6, 10, 14, 0, new List<ItemEntitie> {
            new() { Slot = 0, SlotType = 3, ItemId = 1839, App = 1839, MinimalValue = 120, MaxValue = 120 },
            new() { Slot = 0, SlotType = 5, ItemId = 1899, App = 1899, MinimalValue = 120, MaxValue = 120 },
            new() { Slot = 0, SlotType = 6, ItemId = 1034, App = 1034, MinimalValue = 140, MaxValue = 140 },
            new() { Slot = 0, SlotType = 7, ItemId = 1309, App = 1309, MinimalValue = 120, MaxValue = 120 }
        }),

        // Atirador
        [2] = (8, 9, 16, 12, 5, new List<ItemEntitie> {
            new() { Slot = 0, SlotType = 3, ItemId = 1959, App = 1959, MinimalValue = 80, MaxValue = 80 },
            new() { Slot = 0, SlotType = 5, ItemId = 2019, App = 2019, MinimalValue = 80, MaxValue = 80 },
            new() { Slot = 0, SlotType = 6, ItemId = 1209, App = 1209, MinimalValue = 160, MaxValue = 160 }
        }),

        // Pistoleira
        [3] = (8, 10, 14, 12, 6, new List<ItemEntitie> {
            new() { Slot = 0, SlotType = 3, ItemId = 2079, App = 2079, MinimalValue = 80, MaxValue = 80 },
            new() { Slot = 0, SlotType = 5, ItemId = 2139, App = 2139, MinimalValue = 80, MaxValue = 80 },
            new() { Slot = 0, SlotType = 6, ItemId = 1174, App = 1174, MinimalValue = 140, MaxValue = 140 }
        }),

        // Feiticeiro
        [4] = (7, 16, 9, 8, 10, new List<ItemEntitie> {
            new() { Slot = 0, SlotType = 3, ItemId = 2199, App = 2199, MinimalValue = 60, MaxValue = 60 },
            new() { Slot = 0, SlotType = 5, ItemId = 2259, App = 2259, MinimalValue = 60, MaxValue = 60 },
            new() { Slot = 0, SlotType = 6, ItemId = 1279, App = 1279, MinimalValue = 160, MaxValue = 160 }
        }),

        // Clériga
        [5] = (7, 15, 10, 9, 9, new List<ItemEntitie> {
            new() { Slot = 0, SlotType = 3, ItemId = 2319, App = 2319, MinimalValue = 60, MaxValue = 60 },
            new() { Slot = 0, SlotType = 5, ItemId = 2379, App = 2379, MinimalValue = 60, MaxValue = 60 },
            new() { Slot = 0, SlotType = 6, ItemId = 1244, App = 1244, MinimalValue = 140, MaxValue = 140 }
        })
    };

    private static ushort GetClassInfo(byte classValue) {
        return classValue switch {
            >= 1 and <= 19 => 0,
            >= 20 and <= 29 => 1,
            >= 30 and <= 39 => 2,
            >= 40 and <= 49 => 3,
            >= 50 and <= 59 => 4,
            >= 60 and <= 69 => 5,
            _ => throw new ArgumentException("Classe inválida")
        };
    }

    private static void ApplyInitialCharacterStatus(CharacterEntitie character, (int Strength, int Intelligence, int Agility, int Constitution, int Luck, List<ItemEntitie> Items) config) {
        character.Strength = (uint)config.Strength;
        character.Intelligence = (uint)config.Intelligence;
        character.Agility = (uint)config.Agility;
        character.Constitution = (uint)config.Constitution;
        character.Luck = (uint)config.Luck;

        foreach(var item in config.Items) {
            EquipCharacter(character, item);
        }
    }

    private static void SetCharacterPosition(CharacterEntitie character, uint local) {
        if(local == 0) { // Regenshien
            character.PositionX = 3450;
            character.PositionY = 690;
        }
        else if(local == 1) { // Verband
            character.PositionX = 3470;
            character.PositionY = 935;
        }
    }

    private static void EquipCharacter(CharacterEntitie character, ItemEntitie item) {
        if(character.Equips[item.Slot] == null) {
            character.Equips[item.Slot] = new ItemEntitie();
        }

        character.Equips[item.Slot].OwnerId = character.OwnerAccountId;
        character.Equips[item.Slot].ItemId = item.ItemId;
        character.Equips[item.Slot].Slot = item.Slot;
        character.Equips[item.Slot].SlotType = item.SlotType;
        character.Equips[item.Slot].App = item.App;
        character.Equips[item.Slot].Identification = item.Identification;
        character.Equips[item.Slot].MinimalValue = item.MinimalValue;
        character.Equips[item.Slot].MaxValue = item.MaxValue;
        character.Equips[item.Slot].Refine = item.Refine;
        character.Equips[item.Slot].Time = item.Time;
    }

    private static void SendToCharactersList(Session session, AccountEntitie account) {
        account.Characters = DatabaseHandler.GetCharactersByAccountIdAsync(account.Id).Result;

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

                packet.Write((ushort)(character?.Equips?.ElementAtOrDefault(0)?.ItemId ?? (ushort)0));
                packet.Write((ushort)(character?.Equips?.ElementAtOrDefault(1)?.ItemId ?? (ushort)0));

                // CHEGA NULO
                for(int k = 2; k < 8; k++) {
                    if(character?.Equips != null && character.Equips.Count > k) {
                        packet.Write((ushort)character.Equips[k].App);
                    }
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
            if(character != null) {
                packet.Write((uint)character.Deleted); // Deleted?
            }
            else {
                packet.Write((uint)0);
            }

            packet.Write((byte)(character?.NumericErrors ?? 0)); // Numeric Erros
            //packet.Write((byte)(character?.NumericErrors ?? 0)); // Numeric Erros

            if(!string.IsNullOrEmpty(character?.NumericToken)) {
                packet.Write(true); // Numeric Registered?
            }
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

    // TO-DO: Mapear Personagem (Item, Buffs...)
    // Tratar erro de numericas
    private static void HandleSendToWorld(Session session, StreamHandler stream) {
        var characterSlot = stream.ReadBytes(4)[0];
        var numericRequestChange = stream.ReadBytes(4)[0];
        var numeric1 = Encoding.ASCII.GetString(stream.ReadBytes(4));
        var numeric2 = Encoding.ASCII.GetString(stream.ReadBytes(4));

        var account = session.ActiveAccount;

        var character = DatabaseHandler.GetCharactersByAccountIdAsync(account.Id).Result[characterSlot];

        if(character == null)
            return;

        if(numericRequestChange == 0 && string.IsNullOrEmpty(character?.NumericToken)) {
            character.NumericToken = numeric1;
            character.NumericErrors = 0;
            var numericSaved = DatabaseHandler.SaveCharacterNumeric(character.Name, numeric1, (int)character.NumericErrors).Result;

            if(!numericSaved)
                Console.WriteLine($"{character.Name} -> Erro ao registrar numérica!");

            Console.WriteLine($"{character.Name} -> Numérica Registrada!");
            character.FirstLogin = 0;
        }
        else if(numericRequestChange == 1 && character.NumericToken == numeric1) {
            // TO-DO: Mapear e enviar para o mundo
            Console.WriteLine($"{character.Name} -> Numérica Correta!");

            if(character.FirstLogin == 0) {
                character.FirstLogin = 1;
            }
        }
        else if(numericRequestChange == 2 && character.NumericToken == numeric2) {
            character.NumericToken = numeric1;
            character.NumericErrors = 0;
            var numericaSaved = DatabaseHandler.SaveCharacterNumeric(character.Name, numeric1, (int)character.NumericErrors).Result;

            if(!numericaSaved)
                Console.WriteLine($"{character.Name} -> Erro ao trocar numérica!");

            Console.WriteLine($"{character.Name} -> Numérica Alterada!");
        }
        else {
            Console.WriteLine($"{character.Name} -> Numérica errada!");
            SendToCharactersList(session, account);
            SendClientMessage(session, 16, 0, "Numerica incorreta");
            character.NumericErrors += 1;
            return;
        }

        var packet = PacketFactory.CreateHeader(0x925, 0x7535);

        packet.Write((uint)account.Id); // AccountSerial

        packet.Write((uint)1); // AccountId
        packet.Write((byte)character.FirstLogin); // First Login
        packet.Write((uint)character.Id); // CharacterId

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

        packet.Write((uint)character.MaxHealth); // Max HP
        packet.Write((uint)character.CurrentHealth); // Current HP
        packet.Write((uint)character.MaxMana); // Max Mana
        packet.Write((uint)character.CurrentMana); // Current Mana

        packet.Write((uint)DateTime.UtcNow.AddDays(1).Ticks); // TO-DO: Server reset time

        packet.Write((uint)character.Honor); // Honor
        packet.Write((uint)character.KillPoint); // KillPoint
        packet.Write((uint)character.Infamia); // Infamia
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

        packet.Write((Int64)character.Experience);    // Exp
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

        packet.Write((uint)character.Gold);  // Gold

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

        packet.Write((ushort)(0)); // Active Title

        packet.Write((uint)(0)); // Null_8

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

    private static void Teleport(Session session, uint positionX, uint positionY) {
        var packet = PacketFactory.CreateHeader(0x301, (ushort)session.ActiveCharacter.OwnerAccountId);

        packet.Write((Single)positionX);
        packet.Write((Single)positionY);

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

    //TO-DO: Enviar atributos do mundo para o personagem
    private static void SendToWorldSends(Session session) {
        if(session.ActiveCharacter == null) {
            SendClientMessage(session, 16, 0, "Erro ao carregar personagem");
            return;
        }

        //Enviar criação do personagem no mundo
        CreateCharacterMob(session, 0);

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

    private static void CreateCharacterMob(Session session, byte spawnType) {
        if(session == null) {
            return;
        }

        var account = session.ActiveAccount;
        var character = session.ActiveCharacter;

        SpawnCharacter(session, account, character);

        var packet = PacketFactory.CreateHeader(0x349, (ushort)account.Id);

        packet.Write(Encoding.ASCII.GetBytes(character?.Name?.PadRight(16, '\0')));

        for(int i = 0; i < 8; i++) {
            packet.Write((ushort)0); // TO-DO: Equips
        }

        for(int i = 0; i < 12; i++) {
            packet.Write((byte)0); // TO-DO: ItemEffect
        }

        // Talvez seja assim!?
        packet.Write((Single)character.PositionX);
        packet.Write((Single)character.PositionY);

        packet.Write((uint)character.Rotation);
        packet.Write((uint)character.MaxHealth);
        packet.Write((uint)character.MaxMana);
        packet.Write((uint)character.CurrentHealth);
        packet.Write((uint)character.CurrentMana);

        packet.Write((byte)0x0A); // Unk

        packet.Write((byte)character.SpeedMove);
        packet.Write(spawnType); // TO-DO: SpawnType
        packet.Write((byte)character.Height);
        packet.Write((byte)character.Trunk);
        packet.Write((byte)character.Leg);
        packet.Write((byte)character.Body);
        packet.Write((byte)0); // IsService (0 Player, 1 Npc?!)

        packet.Write((ushort)0); // TO-DO: EffectType
        packet.Write((ushort)0); // TO-DO: SetBuffs

        for(int i = 0; i < 60; i++) {
            packet.Write((ushort)0); // TO-DO: Buffs
        }

        for(int i = 0; i < 60; i++) {
            packet.Write((uint)0); // TO-DO: BuffTime
        }

        packet.Write(Encoding.ASCII.GetBytes(new string('\0', 32))); // BuffTime

        packet.Write((ushort)account.Nation * 4096); // TO-DO: Guild Index and Nation Index

        for(int i = 0; i < 4; i++) {
            if(i == 1) {
                packet.Write((ushort)0x1D); // Sei lá o que diabos é isto
            }
            else {
                packet.Write((ushort)0); // TO-DO: Effects?
            }
        }

        packet.Write((byte)0); // Unk
        packet.Write((byte)0); // TO-DO: ChaosPoints

        packet.Write((long)0); // Null1

        packet.Write((byte)0); // TitleId
        packet.Write((byte)0); // TitleLevel

        packet.Write((ushort)0); // Null2

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine($"OK -> CreateCharacterMob");
    }

    private static void SpawnCharacter(Session session, AccountEntitie account, CharacterEntitie character) {
        var packet = PacketFactory.CreateHeader(0x35E, (ushort)account.Id);

        for(int i = 0; i < 8; i++) {
            packet.Write((ushort)0); // TO-DO: Equips
        }

        packet.Write((Single)character.PositionX);
        packet.Write((Single)character.PositionY);

        packet.Write((ushort)character.Rotation);

        packet.Write((uint)character.MaxHealth);
        packet.Write((uint)character.MaxMana);
        packet.Write((uint)character.CurrentHealth);
        packet.Write((uint)character.CurrentMana);

        packet.Write((ushort)0); // Unk_0

        packet.Write((ushort)character.Level); // Level

        packet.Write((ushort)character.Level); // Null_0

        packet.Write((ushort)0); // IsService

        for(int i = 0; i < 4; i++) {
            packet.Write((byte)0); // TO-DO: Effects
        }

        packet.Write((byte)0); // SpawnType

        packet.Write((byte)character.Height);
        packet.Write((byte)character.Trunk);
        packet.Write((byte)character.Leg);

        packet.Write((ushort)character.Body);

        packet.Write((byte)0); // Mob Type

        packet.Write((byte)account.Nation); // Nation

        packet.Write((ushort)0x7535); // Mob Name?!

        for(int i = 0; i < 3; i++) {
            packet.Write((ushort)0); // Unk_1
        }

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);

        Console.WriteLine("OK -> SpawnCharacter");
    }

    private static async Task ChangeCharacterRequest(Session session) {
        var account = await DatabaseHandler.GetAccountByUsernameAsync(session.Username);

        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        SendToCharactersList(session, account);
    }

    //TO-DO: Deixar visivel para todos/proximos
    private static void UpdateRotation(StreamHandler stream, Session session) {
        var rotation = BitConverter.ToUInt32(stream.ReadBytes(4));

        Console.WriteLine($"Rotation: {rotation}");

        if(session.ActiveCharacter.Rotation != rotation)
            session.ActiveCharacter.Rotation = rotation;
    }

    private static void SendClientMessage(Session session, byte type1, byte type2, string message) {
        var packet = PacketFactory.CreateHeader(0x984);

        packet.Write((byte)0); // Null1
        packet.Write((byte)type1); // Type1
        packet.Write((byte)type2); // Type2
        packet.Write((byte)0); // Null2
        packet.Write(Encoding.ASCII.GetBytes(message?.PadRight(128, '\0')));

        PacketFactory.FinalizePacket(packet);

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);
        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }
}
