namespace Shared.Models.Item;

public class ItemEntitie {
    public uint OwnerId { get; set; }
    public uint Id { get; set; }
    public uint App { get; set; }
    public long Identification { get; set; }
    public List<ItemEffect> ItemEffects { get; set; }
    public uint MaxValue { get; set; }
    public uint MinimalValue { get; set; }
    public byte Slot { get; set; }
    public byte SlotType { get; set; }
    public ushort Refine { get; set; }
    public uint Time { get; set; }
}