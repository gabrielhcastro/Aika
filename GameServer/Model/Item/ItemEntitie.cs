
namespace GameServer.Model.Item;
public class ItemEntitie {
    public uint OwnerId { get; set; }
    public uint ItemId { get; set; }
    public uint App { get; set; }
    public long Identification { get; set; }
    public List<ItemEffect> ItemEffects { get; set; }
    public uint MaxValue { get; set; }
    public uint MinimalValue { get; set; }
    public byte Slot { get; set; }
    public byte SlotType { get; set; }
    public ushort Refine { get; set; }
    public uint Time { get; set; }

    internal ItemEntitie Clone() {
        return new ItemEntitie {
            OwnerId = this.OwnerId,
            ItemId = this.ItemId,
            App = this.App,
            Identification = this.Identification,
            ItemEffects = this.ItemEffects.Select(effect => new ItemEffect {
                Id = (byte[])effect.Id.Clone(),
                Value = (byte[])effect.Value.Clone()
            }).ToList(),
            MaxValue = this.MaxValue,
            MinimalValue = this.MinimalValue,
            Slot = this.Slot,
            SlotType = this.SlotType,
            Refine = this.Refine,
            Time = this.Time
        };
    }
}