using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shared.Models.Character;

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public class CharactersListData {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string Name;

    public ushort Nation;
    public ushort ClassInfo;
    public CharacterSizes Size;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public ushort[] Equip;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public byte[] Refine;

    public CharacterAttributes Attributes;
    public ushort Level;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    public byte[] Null_1;

    public long Exp;
    public long Gold;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Null_2;

    public uint DeleteTime;
    public byte NumericError;
    public bool NumRegister;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    public byte[] NotUse;
}
