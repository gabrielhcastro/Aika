namespace GameServer.GameLogic.Character;

public class CharacterData {
    public string Name { get; set; }
    public int Nation { get; set; }
    public ushort[] Equip { get; set; } = new ushort[8];
    public byte Refine { get; set; }
    public uint Gold { get; set; }
    public uint Exp { get; set; }
    public byte ClassInfo { get; set; }
    public byte Level { get; set; }
    public uint DeleteTime { get; set; }
}