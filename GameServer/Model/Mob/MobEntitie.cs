using GameServer.Model.World;

namespace GameServer.Model.Mob;
internal class MobEntitie {
    public uint Index;
    public string Name;
    public ushort NameValue;
    public ushort[] Equips;
    public Position Position;
    public byte Height;
    public byte Trunk;
    public byte Leg;
    public uint PhysicAtack;
    public uint PhysicDefense;
    public uint MagicAtack;
    public uint MagicDefense;
    public ushort MoveSpeed;
    public uint ExpReward;
    public ushort MobLevelDifference;
    public uint InitialHealth;
    public uint Rotation;
    public ushort MobType;
    public bool IsService;
    public byte SpawnType;
    public ushort CntControl;
    public ushort RespawnTime;
    public ushort Skill01;
    public ushort Skill02;
    public ushort Skill03;
    public ushort Skill04;
    public ushort Skill05;
    public ushort DropIndex;
    public bool IsActiveToSpawn;
    public ushort DungeonDropIndex;
}