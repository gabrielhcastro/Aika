namespace Shared.Models.Item;

public class ItemEntitie {
    public uint Id { get; set; }
    public uint App { get; set; }
    public long Identification { get; set; }
    public List<ItemEffect> ItemEffects { get; set; }
    public byte MaxValue { get; set; }
    public byte MinValue { get; set; }
    public ushort Refine { get; set; }
    public uint Time { get; set; }
}