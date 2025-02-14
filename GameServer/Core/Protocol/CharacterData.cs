namespace GameServer.Core.Protocol;

public class CharacterData {
    public string Name { get; set; } // Nome do personagem (16 bytes)
    public int Nation { get; set; } // Nação do personagem
    public ushort[] Equip { get; set; } = new ushort[8]; // Equipamentos (8 slots)
    public byte Refine { get; set; } // Refinamento do equipamento
    public uint Gold { get; set; } // Quantidade de ouro
    public uint Exp { get; set; } // Experiência do personagem
    public byte ClassInfo { get; set; } // Classe do personagem
    public byte Level { get; set; } // Nível do personagem
    public uint DeleteTime { get; set; } // Tempo de exclusão do personagem (em UNIX time)
}