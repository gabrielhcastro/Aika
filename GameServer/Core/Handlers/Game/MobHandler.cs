using GameServer.Core.Base;
using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Model.Mob;
using GameServer.Service;

namespace GameServer.Core.Handlers.Game;
internal class MobHandler {
    public static void CreateMob(Session session, CharacterEntitie character, ushort id, byte spawnType) {
        if(session == null) return;

        var packet = MobService.CreateCharacterMobPacket(character, id, spawnType);
        session.SendPacket(packet);
    }

    public static void SpawnMob(Session session, byte mobId) {
        var mob = new MobEntitie() {
            Index = mobId,
            Name = "Max Filhote",
            NameValue = mobId,
            MobType = 1025,
            MobLevelDifference = 0,
            RespawnTime = 45,
            Equips = new ushort[8],
            Position = new(3450, 845),
            InitialHealth = 207,
            PhysicAtack = 17,
            PhysicDefense = 25,
            MagicAtack = 18,
            MagicDefense = 17,
            ExpReward = 23,
            DropIndex = 0,
            MoveSpeed = 40,
            SpawnType = 0,
            IsService = true,
            Height = 7,
            Trunk = 119,
            Leg = 119,
            Skill01 = 0,
            Skill02 = 0,
            Skill03 = 0,
            Skill04 = 0,
            IsActiveToSpawn = true
        };

        mob.Equips[0] = 316;
        mob.Equips[1] = 0;
        mob.Equips[6] = 0;

        var packet = MobService.CreateSpawnMobPacket(mob);
        session.SendPacket(packet);
    }

    public static void RemoveCharacterMob(Session otherSession) {
        Console.WriteLine($"Tentando remover {otherSession.ActiveCharacter.Name}");
    }
}
