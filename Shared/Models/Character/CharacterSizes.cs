using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models.Character;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class CharacterSizes {
    public byte Altura;
    public byte Tronco;
    public byte Perna;
    public byte Corpo;
}
