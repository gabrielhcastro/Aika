using System.Runtime.InteropServices;

namespace Shared.Models.Character;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CharacterAttributes {
    public ushort Strength;
    public ushort Agility;
    public ushort Inteligence;
    public ushort Constitution;
    public ushort Luck;
    public ushort Status;
}