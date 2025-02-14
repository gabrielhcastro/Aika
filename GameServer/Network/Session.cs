using GameServer.Core.Base;
using NLog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GameServer.Network;

public class Session : IDisposable {
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    private readonly INetwork _network;
    private readonly Dictionary<string, object> _attributes = [];
    private readonly SocketAsyncEventArgs _writeEventArg = new();
    private ConcurrentQueue<byte[]> _packetQueue = new();
    private readonly object _lock = new();
    private bool _processing;
    private bool _sending;
    private bool _closed;
    private IPEndPoint RemoteEndPoint => (IPEndPoint)Socket.RemoteEndPoint;
    public uint Id { get; }
    public Socket Socket { get; }
    public SocketAsyncEventArgs ReadEventArg { get; }
    public IPAddress Ip { get; }
    public DateTime LastActivity { get; set; }
    //public DateTime LastPingTime { get; set; } = DateTime.UtcNow;
    //public DateTime LastPongTime { get; set; } = DateTime.UtcNow;

    public Session(INetwork network, SocketAsyncEventArgs readEventArg, Socket socket) {
        Socket = socket;
        Id = (uint)RemoteEndPoint.GetHashCode();
        _network = network;
        ReadEventArg = readEventArg;
        _writeEventArg.Completed += WriteComplete;
        Ip = RemoteEndPoint.Address;
        ProcessPackets();
    }

    public void SendPacket(byte[] packet) {
        if(_packetQueue == null)
            return;

        _packetQueue.Enqueue(packet);
        lock(_lock) {
            if(!_processing) {
                _processing = true;
                Task.Run(ProcessPackets);
            }
        }

        lock(Socket) {
            if(!_sending)
                ProcessPackets();
        }
    }

    public void AddAttribute(string name, object attribute) {
        _attributes.Add(name, attribute);
        Console.WriteLine($"Adding Attributes. Key: {name} Value:{attribute}");
    }

    public object GetAttribute(string name) {
        _attributes.TryGetValue(name, out var attribute);
        Console.WriteLine($"Getting Attributes. Key: {name} Value:{attribute}");
        return attribute;
    }

    private byte[] GetNextPacket() {
        if(_packetQueue == null)
            return null;
        _packetQueue.TryDequeue(out var result);
        Console.WriteLine($"Getting Next Packet");
        return result;
    }

    private void ProcessPackets() {
        lock(Socket) {
            _sending = true;
        }

        var buffer = GetNextPacket();
        if(buffer == null) {
            lock(Socket) {
                _sending = false;
            }

            return;
        }

        _writeEventArg.SetBuffer(buffer, 0, buffer.Length);

        try {
            var willRaise = Socket.SendAsync(_writeEventArg);
            if(!willRaise)
                ProcessSend(_writeEventArg);
        }
        catch(ObjectDisposedException) {
            _packetQueue = null;
            lock(Socket) {
                _sending = false;
            }
        }
    }

    private void WriteComplete(object sender, SocketAsyncEventArgs e) {
        switch(e.LastOperation) {
            case SocketAsyncOperation.Send:
            ProcessSend(e);
            break;
            default:
            throw new ArgumentException("The last operation completed on the socket was not a send");
        }
    }

    private void ProcessSend(SocketAsyncEventArgs e) {
        if(e.SocketError == SocketError.Success) {
            _network.OnSend(this, e.Buffer, e.Offset, e.BytesTransferred);
            ProcessPackets();
        }
        else {
            _log.Error("Error on ProcessSend: {0}", e.SocketError.ToString());
            Close();
        }
    }

    public void Close() {
        if(_closed)
            return;

        _closed = true;
        _packetQueue = null;
        _network.OnDisconnect(this);
        try {
            Socket.Shutdown(SocketShutdown.Receive);
        }
        catch(Exception) {
            // ignored
        }

        Socket.Close();
        _network.RemoveSession(this);
    }

    public void Dispose() {
        _writeEventArg?.Dispose();
        GC.SuppressFinalize(this);
    }
}