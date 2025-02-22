using GameServer.Core.Base;
using Shared.Handlers;
using Shared.Models.Account;
using Shared.Models.Character;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

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
            //await HandleSendToWorld(session, stream);
            SendClientMessage(session, 16, 0, "Em desenvolvimento.");
            break;
            case 0xF0B:
            await SendToWorldSends(session);
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
        session.Username = username;

        var account = await DatabaseHandler.GetAccountByUsernameAsync(username);
        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        await SendToCharacterList(session, account);

        Console.WriteLine($"OK -> ChararactersList");
    }

    private static async Task HandleCreateCharacter(Session session, StreamHandler stream) {
        CharacterEntitie character = new() {
            OwnerAccountId = BitConverter.ToUInt32(stream.ReadBytes(4), 0)
        };

        var account = await DatabaseHandler.GetAccountByUsernameAsync(session.Username);
        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        var slot = BitConverter.ToUInt32(stream.ReadBytes(4), 0);
        if(account?.Characters?.Count == 3 || slot > 2) {
            SendClientMessage(session, 16, 0, "SLOT_ERROR ou Você já tem 3 personagens.");
            return;
        }

        character.Slot = slot;

        // TO-DO: Verificar se o nome já existe e se contem caracteres permitidos
        var name = Encoding.ASCII.GetString(stream.ReadBytes(16)).TrimEnd('\0');
        if(name.Length > 14) {
            SendClientMessage(session, 16, 0, "Limitado a 14 caracteres apenas.");
            return;
        }

        character.Name = name;

        var ClassInfo = BitConverter.ToUInt16(stream.ReadBytes(2), 0);
        if(ClassInfo < 10 && ClassInfo > 69)
            SendClientMessage(session, 16, 0, "Classe fora dos limites.");

        character.ClassInfo = ClassInfo;

        var hair = BitConverter.ToUInt16(stream.ReadBytes(2), 0);
        if(hair < 7700 || hair > 7731) // Proteção Criar Itens
            SendClientMessage(session, 16, 0, "Cabelo fora dos limites.");

        _ = Encoding.ASCII.GetString(stream.ReadBytes(12)).TrimEnd('\0');

        var local = BitConverter.ToUInt32(stream.ReadBytes(4), 0);
        if(local == 0) { // Regenshien
            character.PositionX = 3450;
            character.PositionY = 690;
        }
        else if(local == 1) { // Verband
            character.PositionX = 3470;
            character.PositionY = 935;
        }

        bool success = await DatabaseHandler.CreateCharacterAsync(character, account.Id);
        if(!success) {
            SendClientMessage(session, 16, 0, "Erro ao criar personagem.");
            return;
        }

        Console.WriteLine($"Personagem criado: {character.Name} no slot {character.Slot}");

        await SendToCharacterList(session, account);
    }

    private static async Task SendToCharacterList(Session session, AccountEntitie account) {
        account.Characters = await DatabaseHandler.GetCharactersByAccountIdAsync(account.Id);

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
                packet.Write((byte)character.Height); // Altura
                packet.Write((byte)character.Trunk); // Tronco
                packet.Write((byte)character.Leg); // Perna
                packet.Write((byte)character.Body); // Corpo
            }

            for(int k = 0; k < 8; k++) {
                packet.Write((ushort)0); //Equipamento
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

            if(character != null) {
                packet.Write((uint)character.Deleted); // Deleted?
            }
            else {
                packet.Write((uint)0);
            }

            packet.Write((byte)(character?.NumericErrors ?? 0)); // Numeric Erros

            packet.Write(true); // Numeric Registered?

            packet.Write(new byte[6]); // NotUse

            if(!string.IsNullOrEmpty(character?.Name))
                Console.WriteLine($"Character: {character.Name} -> Carregado");
        }

        byte[] packetData = packet.GetBytes();
        EncDec.Encrypt(ref packetData, packetData.Length);

        session.SendPacket(packetData);

        PacketPool.Return(packet);
    }

    // TO-DO: Mapear Personagem e adicionar na sessão o personagem ativo
    // Tratar registro e troca de numerica
    private static async Task HandleSendToWorld(Session session, StreamHandler stream) {
        var characterSlot = BitConverter.ToString(stream.ReadBytes(4), 0)[0];
        var numericRequestChange = BitConverter.ToString(stream.ReadBytes(4), 0)[0];
        var numeric1 = Encoding.ASCII.GetString(stream.ReadBytes(4));
        var numeric2 = Encoding.ASCII.GetString(stream.ReadBytes(4));

        var account = await DatabaseHandler.GetAccountByUsernameAsync(session.Username);
        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        var character = account.Characters.ToList()[characterSlot];

        //if(charactersList[characterSlot] == null)
        //    return;

        if(numericRequestChange == '0' && string.IsNullOrEmpty(character.NumericToken)) {
            // TO-DO: Salvar no banco de dados
            character.NumericToken = numeric1;
            character.NumericErrors = 0;
        }
        else if(numericRequestChange == '1' && character.NumericToken == numeric1) {
            // Verificando a numerica
            // TO-DO: Mapear e enviar para o mundo
        }
        else if(numericRequestChange == '2' && character.NumericToken == numeric2) {
            // TO-DO: Salvar no banco de dados
            character.NumericToken = numeric1;
            character.NumericErrors = 0;
        }

        var packet = PacketFactory.CreateHeader(0x925, 0x7535);

        packet.Write((uint)account.Id); // AccountSerial

        packet.Write((uint)1); // AccountId
        packet.Write((byte)character.FirstLogin); // First Login
        packet.Write((uint)character.Id); // CharacterId

        packet.Write(Encoding.ASCII.GetBytes("Vitor".PadRight(16, '\0'))); // Name

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

        packet.Write((ushort)0); // Critical
        packet.Write((byte)0); // Miss
        packet.Write((byte)0); // Accuracy

        packet.Write((ushort)0);     // Null_3

        packet.Write((Int64)1);    // Exp
        packet.Write((ushort)1);     // Level
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

        packet.Write((Int64)0);  // Gold

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

        packet.Write((ushort)0); // TitleProgressType9[0]
        packet.Write((ushort)0); // TitleProgressType9[1]
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

        Console.WriteLine($"OK -> ChararactersList");
    }

    //TO-DO: Enviar atributos do mundo para o personagem
    private static async Task SendToWorldSends(Session session) {
        //if(session.Character == null) {
        //    SendClientMessage(session, 16, 0, "Erro ao carregar personagem.");
        //    return;
        //}

        //Console.WriteLine($"Carregando personagem {session.Character.Name}...");

        // Enviar criação do personagem no mundo
        //CreateCharacterMob(session, 0, 1);

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

        Console.WriteLine($"Personagem carregado com sucesso!");
    }

    private static void CreateCharacterMob(Session session, byte spawnType, uint mobId) {
        if(session == null) {
            return;
        }

        var packet = PacketFactory.CreateHeader(0x35E, (ushort)mobId);

        packet.Write(Encoding.ASCII.GetBytes(session?.Character?.Name?.PadRight(16, '\0') ?? new string('\0', 16)));

        for(int i = 0; i < 8; i++) {
            packet.Write((ushort)0); // TO-DO: Equips
        }

        for(int i = 0; i < 12; i++) {
            packet.Write((byte)0); // TO-DO: ItemEffect
        }

        // Talvez seja assim!?
        packet.Write((Single)session.Character.PositionX);
        packet.Write((Single)session.Character.PositionY);

        packet.Write((uint)session.Character.Rotation);
        packet.Write((uint)session.Character.MaxHealth);
        packet.Write((uint)session.Character.MaxMana);
        packet.Write((uint)session.Character.CurrentHealth);
        packet.Write((uint)session.Character.CurrentMana);

        packet.Write((byte)0); // Unk

        packet.Write((byte)session.Character.SpeedMove);
        packet.Write(spawnType); // TO-DO: SpawnType
        packet.Write((byte)session.Character.Height);
        packet.Write((byte)session.Character.Trunk);
        packet.Write((byte)session.Character.Leg);
        packet.Write((byte)session.Character.Body);
        packet.Write(false); // IsService 

        packet.Write((ushort)0); // TO-DO: EffectType
        packet.Write((ushort)0); // TO-DO: SetBuffs

        for(int i = 0; i < 60; i++) {
            packet.Write((uint)0); // TO-DO: Buffs
        }

        for(int i = 0; i < 60; i++) {
            packet.Write((ushort)0); // TO-DO: BuffTime
        }

        packet.Write(Encoding.ASCII.GetBytes(new string('\0', 32))); // BuffTime

        packet.Write((ushort)0); // TO-DO: Guild Index and Nation Index

        for(int i = 0; i < 4; i++) {
            packet.Write((ushort)0); // TO-DO: Effects?
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
    }

    private static async Task ChangeCharacterRequest(Session session) {
        var account = await DatabaseHandler.GetAccountByUsernameAsync(session.Username);

        if(account == null) {
            Console.WriteLine("Conta não encontrada.");
            return;
        }

        await SendToCharacterList(session, account);
    }

    //TO-DO: Deixar visivel para todos/proximos
    private static void UpdateRotation(StreamHandler stream, Session session) {
        if(session.Character.Rotation == stream.ReadUInt32())
            return;

        session.Character.Rotation = stream.ReadUInt32();
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
