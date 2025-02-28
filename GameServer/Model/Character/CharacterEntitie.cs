using GameServer.Model.Item;
using GameServer.Model.World;
using System.Collections.Concurrent;

namespace GameServer.Model.Character;
public class CharacterEntitie {
    public uint Id { get; set; }
    public ushort ConnectionId { get; set; }
    public uint OwnerAccountId { get; set; }
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
    public uint Strength { get; set; }
    public uint Agility { get; set; }
    public uint Intelligence { get; set; }
    public uint Constitution { get; set; }
    public uint Luck { get; set; }
    public uint Status { get; set; }
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
    public uint Gold { get; set; }
    public uint PositionX { get; set; }
    public uint PositionY { get; set; }
    public string CreationTime { get; set; }
    public string DeleteTime { get; set; }
    public uint LoginTime { get; set; }
    public uint ActiveTitle { get; set; }
    public uint ActiveAction { get; set; }
    public string TeleportPositions { get; set; }
    public uint PranEvolutionCount { get; set; }
    public uint SavedPositionX { get; set; }
    public uint SavedPositionY { get; set; }
    public ushort Critical { get; set; }
    public uint PhysicDamage { get; set; }
    public uint MagicDamage { get; set; }
    public uint PhysicDefense { get; set; }
    public uint MagicDefense { get; set; }
    public uint BonusDamage { get; set; }
    public uint Miss { get; set; }
    public uint Accuracy { get; set; }

    public ushort[] Skills { get; set; }
    public List<ItemEntitie> Itens { get; set; }
    public List<ItemEntitie> Equips { get; set; }
    public ConcurrentDictionary<ushort, uint> Buffs { get; set; }
    public Position Position { get; set; }
}