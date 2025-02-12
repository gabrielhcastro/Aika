using NLog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Shared.Network;

public class Session : IDisposable
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    private readonly INetwork _network;
    private readonly Dictionary<string, object> _attributes = [];
    private readonly SocketAsyncEventArgs _writeEventArg = new();
    private ConcurrentQueue<byte[]> _packetQueue = new();
    private bool _sending;
    private bool _closed;
    private IPEndPoint RemoteEndPoint => (IPEndPoint) Socket.RemoteEndPoint;
    public uint Id { get; }
    public Socket Socket { get; }
    public SocketAsyncEventArgs ReadEventArg { get; }
    public IPAddress Ip { get; }

    public Session(INetwork network, SocketAsyncEventArgs readEventArg, Socket socket)
    {
        Socket = socket;
        Id = (uint) RemoteEndPoint.GetHashCode();
        _network = network;
        ReadEventArg = readEventArg;
        _writeEventArg.Completed += WriteComplete;
        Ip = RemoteEndPoint.Address;
        ProccessPackets();
    }

    public void SendPacket(byte[] packet)
    {
        if (_packetQueue == null)
            return;
        _packetQueue.Enqueue(packet);
        _log.Debug($"Sending Packet: {packet}");
        lock (Socket)
        {
            if (!_sending)
                ProccessPackets();
        }
    }

    public void AddAttribute(string name, object attribute)
    {
        _attributes.Add(name, attribute);
        _log.Debug($"Adding Attributes. Key: {name} Value:{attribute}");
    }

    public object GetAttribute(string name)
    {
        _attributes.TryGetValue(name, out var attribute);
        _log.Debug($"Getting Attributes. Key: {name} Value:{attribute}");
        return attribute;
    }

    private byte[] GetNextPacket()
    {
        if (_packetQueue == null)
            return null;
        _packetQueue.TryDequeue(out var result);
        _log.Debug($"Getting Next Packet: {result}");
        return result;
    }

    private void ProccessPackets()
    {
        lock (Socket)
        {
            _sending = true;
        }

        var buffer = GetNextPacket();
        if (buffer == null)
        {
            lock (Socket)
            {
                _sending = false;
            }

            return;
        }

        _writeEventArg.SetBuffer(buffer, 0, buffer.Length);

        try
        {
            var willRaise = Socket.SendAsync(_writeEventArg);
            if (!willRaise)
                ProcessSend(_writeEventArg);
        }
        catch (ObjectDisposedException)
        {
            _packetQueue = null;
            lock (Socket)
            {
                _sending = false;
            }
        }
    }

    private void WriteComplete(object sender, SocketAsyncEventArgs e)
    {
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Send:
                ProcessSend(e);
                break;
            default:
                throw new ArgumentException("The last operation completed on the socket was not a send");
        }
    }

    private void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            _network.OnSend(this, e.Buffer, e.Offset, e.BytesTransferred);
            ProccessPackets();
        }
        else
        {
            _log.Error("Error on ProcessSend: {0}", e.SocketError.ToString());
            Close();
        }
    }

    public void Close()
    {
        if (_closed)
            return;

        _closed = true;
        _packetQueue = null;
        _network.OnDisconnect(this);
        try
        {
            Socket.Shutdown(SocketShutdown.Receive);
        }
        catch (Exception)
        {
            // ignored
        }

        Socket.Close();
        _network.RemoveSession(this);
    }

    public void Dispose()
    {
        _writeEventArg.Dispose();
        GC.SuppressFinalize(this);
    }
}