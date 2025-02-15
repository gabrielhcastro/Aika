using Shared.Core.Conversions;
using System.Text;

namespace GameServer.Handlers;

public class StreamHandler {
    #region Data

    private const int DefaultSize = 128;

    private readonly Encoding _encoding = Encoding.GetEncoding("iso-8859-1");

    #endregion

    #region Properties

    public byte[] Buffer { get; private set; }
    public int Count { get; private set; }
    public int Pos { get; set; }
    private bool IsLittleEndian { get; }
    private EndianBitConverter Converter => IsLittleEndian ? EndianBitConverter.Little : EndianBitConverter.Big;

    #endregion

    #region Operators & Casts

    public byte this[int index] {
        set => Buffer[index] = value;
        get => Buffer[index];
    }

    public static explicit operator StreamHandler(byte[] bytes) {
        return new StreamHandler(bytes);
    }

    public static implicit operator byte[](StreamHandler packet) {
        return packet.GetBytes();
    }

    #endregion

    #region Constructor

    public StreamHandler() : this(DefaultSize) {
    }

    public StreamHandler(int count) {
        IsLittleEndian = true;
        Reserve(count);
    }

    public StreamHandler(byte[] bytes) {
        IsLittleEndian = true;
        Replace(bytes);
    }

    #endregion

    #region Replace

    public StreamHandler Replace(StreamHandler packet) {
        return Replace(packet.Buffer, 0, packet.Count);
    }

    public StreamHandler Replace(byte[] bytes) {
        return Replace(bytes, 0, bytes.Length);
    }

    public StreamHandler Replace(StreamHandler packet, int offset, int count) {
        return Replace(packet.Buffer, offset, count);
    }

    public StreamHandler Replace(byte[] bytes, int offset, int count) {
        Reserve(count);
        var newBuff = new byte[count];
        System.Buffer.BlockCopy(bytes, offset, newBuff, 0, count);
        Buffer = newBuff;
        Count = count;

        return this;
    }

    #endregion // Replace

    #region Misc

    private static byte[] Roundup(int length) {
        var i = 16;
        while(length > i)
            i <<= 1;
        return new byte[i];
    }

    public void Reserve(int count) {
        if(Buffer == null) Buffer = Roundup(count);
        else if(count > Buffer.Length) {
            var newBuffer = Roundup(count);
            System.Buffer.BlockCopy(Buffer, 0, newBuffer, 0, Count);
            Buffer = newBuffer;
        }
    }

    public StreamHandler Insert(int offset, byte[] copyArray) {
        return Insert(offset, copyArray, 0, copyArray.Length);
    }

    public StreamHandler PushBack(byte b) {
        Reserve(Count + 1);
        Buffer[Count++] = b;
        return this;
    }

    public StreamHandler Insert(int offset, byte[] copyArray, int copyArrayOffset, int count) {
        Reserve(Count + count);
        System.Buffer.BlockCopy(Buffer, offset, Buffer, offset + count, Count - offset);
        System.Buffer.BlockCopy(copyArray, copyArrayOffset, Buffer, offset, count);
        Count += count;
        return this;
    }

    #endregion

    #region GetBytes

    public byte[] GetBytes() {
        var temp = new byte[Count];
        System.Buffer.BlockCopy(Buffer, 0, temp, 0, Count);
        return temp;
    }

    #endregion

    #region Write Types

    public StreamHandler Write(bool value, bool isInt = false) {
        return isInt ? Write(value ? 1 : 0) : Write(value ? (byte)1 : (byte)0);
    }

    public StreamHandler Write(byte value) {
        PushBack(value);
        return this;
    }

    public StreamHandler Write(byte[] value, bool appendSize = false) {
        if(appendSize)
            Write((ushort)(value.Length + 2));
        return Insert(Count, value);
    }

    public StreamHandler Write(short value) {
        return Write(Converter.GetBytes(value));
    }

    public StreamHandler Write(int value) {
        return Write(Converter.GetBytes(value));
    }

    public StreamHandler Write(long value) {
        return Write(Converter.GetBytes(value));
    }

    public StreamHandler Write(ushort value) {
        return Write(Converter.GetBytes(value));
    }

    public StreamHandler Write(uint value) {
        return Write(Converter.GetBytes(value));
    }

    public StreamHandler Write(ulong value) {
        return Write(Converter.GetBytes(value));
    }

    public StreamHandler Write(float value) {
        return Write(Converter.GetBytes(value));
    }

    public StreamHandler WriteCc(int count) {
        var buff = new byte[count];
        for(var i = 0; i < count; i++)
            buff[i] = 0xCC;

        return Write(buff);
    }

    //public Packetpacket Write(BasePacket value) {
    //    return value.Write(this);
    //}

    public StreamHandler Write(StreamHandler value, bool appendSize = false) {
        return Write(value.GetBytes(), appendSize);
    }

    public StreamHandler Write(string value, int count, bool fillCc = false) {
        var tmp = _encoding.GetBytes(fillCc ? value + '\u0000' : value);
        if(tmp.Length > count)
            Array.Resize(ref tmp, count);
        var buff = new byte[count];
        if(fillCc) {
            for(var i = 0; i < buff.Length; i++)
                buff[i] = 0xCC;
        }

        System.Buffer.BlockCopy(tmp, 0, buff, 0, tmp.Length);
        return Write(buff);
    }

    #endregion

    #region Read Types

    public bool ReadBoolean(bool isInt = false) {
        return isInt ? ReadUInt32() == 1 : ReadByte() == 1;
    }

    public byte ReadByte() {
        if(Pos + 1 > Count)
            throw new Exception();
        return this[Pos++];
    }

    public byte[] ReadBytes(int count) {
        if(Pos + count > Count)
            throw new Exception();

        var result = new byte[count];
        System.Buffer.BlockCopy(Buffer, Pos, result, 0, count);
        Pos += count;
        return result;
    }

    public short ReadInt16() {
        if(Pos + 2 > Count)
            throw new Exception();

        var result = Converter.ToInt16(Buffer, Pos);
        Pos += 2;

        return result;
    }

    public int ReadInt32() {
        if(Pos + 4 > Count)
            throw new Exception();

        var result = Converter.ToInt32(Buffer, Pos);
        Pos += 4;

        return result;
    }

    public long ReadInt64() {
        if(Pos + 8 > Count)
            throw new Exception();

        var result = Converter.ToInt64(Buffer, Pos);
        Pos += 8;

        return result;
    }

    public ushort ReadUInt16() {
        if(Pos + 2 > Count)
            throw new Exception();

        var result = Converter.ToUInt16(Buffer, Pos);
        Pos += 2;

        return result;
    }

    public uint ReadUInt32() {
        if(Pos + 4 > Count)
            throw new Exception();

        var result = Converter.ToUInt32(Buffer, Pos);
        Pos += 4;

        return result;
    }

    public ulong ReadUInt64() {
        if(Pos + 8 > Count)
            throw new Exception();

        var result = Converter.ToUInt64(Buffer, Pos);
        Pos += 8;

        return result;
    }

    public float ReadSingle() {
        if(Pos + 4 > Count)
            throw new Exception();

        var result = Converter.ToSingle(Buffer, Pos);
        Pos += 4;

        return result;
    }

    public double ReadDouble() {
        if(Pos + 8 > Count)
            throw new Exception();

        var result = Converter.ToDouble(Buffer, Pos);
        Pos += 8;

        return result;
    }

    public string ReadString(int size) {
        var str = ReadBytes(size);
        for(var i = 0; i < str.Length; i++)
            if(str[i].Equals(0xCC))
                str[i] = 0x00;
        return _encoding.GetString(str).Trim('\u0000');
    }

    public string ReadMacAddress() {
        var str = ReadBytes(6);
        return BitConverter.ToString(str).Replace("-", ":");
    }
    #endregion
}