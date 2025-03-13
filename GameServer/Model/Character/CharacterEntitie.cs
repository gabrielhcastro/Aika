using GameServer.Model.Item;
using GameServer.Model.World;
using System.Collections.Concurrent;

namespace GameServer.Model.Character;
public class CharacterEntitie {
    public ushort Id { get; set; }
    public ushort OwnerAccountId { get; set; }
    public string Name { get; set; }
    public byte Slot { get; set; }
    public string NumericToken { get; set; }
    public byte NumericErrors { get; set; }
    public byte Deleted { get; set; }
    public byte SpeedMove { get; set; }
    public ushort Rotation { get; set; }
    public string LastLogin { get; set; }
    public byte PlayerKill { get; set; }
    public byte ClassInfo { get; set; }
    public byte FirstLogin { get; set; }
    public ushort Strength { get; set; }
    public ushort Agility { get; set; }
    public ushort Intelligence { get; set; }
    public ushort Constitution { get; set; }
    public ushort Luck { get; set; }
    public ushort Status { get; set; }
    public byte Height { get; set; }
    public byte Trunk { get; set; }
    public byte Leg { get; set; }
    public byte Body { get; set; }
    public uint CurrentHealth { get; set; }
    public uint MaxHealth { get; set; }
    public uint CurrentMana { get; set; }
    public uint MaxMana { get; set; }
    public bool Deleting { get; set; }
    public uint Honor { get; set; }
    public uint KillPoint { get; set; }
    public byte Infamia { get; set; }
    public uint SkillPoint { get; set; }
    public ulong Experience { get; set; }
    public byte Level { get; set; }
    public uint GuildIndex { get; set; }
    public ulong Gold { get; set; }
    public string CreationTime { get; set; }
    public string DeleteTime { get; set; }
    public uint LoginTime { get; set; }
    public byte ActiveTitle { get; set; }
    public byte ActiveAction { get; set; }
    public string TeleportPositions { get; set; }
    public byte PranEvolutionCount { get; set; }
    public Single PositionX { get; internal set; }
    public Single PositionY { get; internal set; }
    public ushort SavedPositionX { get; set; }
    public ushort SavedPositionY { get; set; }
    public ushort Critical { get; set; }
    public ushort PhysicDamage { get; set; }
    public ushort MagicDamage { get; set; }
    public ushort PhysicDefense { get; set; }
    public ushort MagicDefense { get; set; }
    public ushort BonusDamage { get; set; }
    public byte Miss { get; set; }
    public byte Accuracy { get; set; }
    public bool IsActive { get; set; }

    public HashSet<ushort> Skills { get; set; }
    public List<ItemEntitie> Equips { get; set; }
    public List<ItemEntitie> Inventory { get; set; }
    public ConcurrentDictionary<ushort, uint> Buffs { get; set; }
    public Position Position { get; set; }
    public HashSet<ushort> VisiblePlayers { get; set; } = [];
    public HashSet<ushort> VisibleMobs { get; set; }
    public HashSet<ushort> VisibleNpcs { get; set; }
    public HashSet<Neighbors> Neighbors { get; set; }


}