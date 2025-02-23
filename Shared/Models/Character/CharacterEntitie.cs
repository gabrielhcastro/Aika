using Shared.Models.Item;
using Shared.Models.World;
using System.Collections.Concurrent;

namespace Shared.Models.Character;
public class CharacterEntitie {
    public uint Id { get; set; }
    public uint OwnerAccountId { get; set; }
    public string Name { get; set; }
    public uint Slot { get; set; }
    public string NumericToken { get; set; }
    public uint NumericErrors { get; set; }
    public byte Deleted { get; set; }
    public uint SpeedMove { get; set; }
    public uint Rotation { get; set; }
    public string LastLogin { get; set; }
    public byte PlayerKill { get; set; }
    public uint ClassInfo { get; set; }
    public uint FirstLogin { get; set; }
    public uint Strength { get; set; }
    public uint Agility { get; set; }
    public uint Intelligence { get; set; }
    public uint Constitution { get; set; }
    public uint Luck { get; set; }
    public uint Status { get; set; }
    public uint Height { get; set; }
    public uint Trunk { get; set; }
    public uint Leg { get; set; }
    public uint Body { get; set; }
    public uint CurrentHealth { get; set; }
    public uint MaxHealth { get; set; }
    public uint CurrentMana { get; set; }
    public uint MaxMana { get; set; }
    public bool Deleting { get; set; }
    public uint Honor { get; set; }
    public uint KillPoint { get; set; }
    public uint Infamia { get; set; }
    public uint SkillPoint { get; set; }
    public ulong Experience { get; set; }
    public uint Level { get; set; }
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
    public ItemEntitie[] Itens { get; set; }
    public ItemEntitie[] Equips { get; set; }
    public ConcurrentDictionary<ushort, uint> Buffs { get; set; }
    public Position Position { get; set; }
}