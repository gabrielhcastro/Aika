using Shared.Models.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Network.Packets;
internal class SendToCharactersListPacket {
    public PacketHeader Header;
    public uint AccountID;
    public uint Unk;
    public uint NotUse;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public CharactersListData[] CharactersData;
}
