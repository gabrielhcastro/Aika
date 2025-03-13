using GameServer.Core.Handlers;
using GameServer.Model.Account;
using GameServer.Model.Character;
using GameServer.Network;
using NLog;
using System.Net;
using System.Net.Sockets;

namespace GameServer.Core.Base;

public class Session : IDisposable {
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly SocketAsyncEventArgs _writeEventArg = new();
    private readonly RingBuffer _packetQueue = new(1024 * 64);
    private INetwork _network;
    public INetwork Network => _network;
    private int _processing;
    private int _sending;
    private int _closed;
    public uint Id { get; private set; }
    public Socket Socket { get; private set; }
    public SocketAsyncEventArgs ReadEventArg { get; private set; }
    public IPAddress Ip { get; private set; }
    public DateTime LastActivity { get; set; }
    public CharacterEntitie ActiveCharacter { get; set; }
    public AccountEntitie ActiveAccount { get; set; }

    public Session() {
        _closed = 0;
        _processing = 0;
        _sending = 0;
        _packetQueue.Reset();
        LastActivity = DateTime.Now;
    }

    public void Init(INetwork network, SocketAsyncEventArgs readEventArg, Socket socket) {
        _network = network;
        ReadEventArg = readEventArg;
        Socket = socket;
        _closed = 0;
        _processing = 0;
        _sending = 0;
        _packetQueue.Reset();
        LastActivity = DateTime.Now;

        Id = (uint)(socket?.RemoteEndPoint?.GetHashCode() ?? 0);
        Ip = (socket != null) ? ((IPEndPoint)socket.RemoteEndPoint).Address : IPAddress.None;

        _writeEventArg.Completed -= WriteComplete;
        _writeEventArg.Completed += WriteComplete;
    }

    public void Reset() {
        _closed = 1;
        _processing = 0;
        _sending = 0;
        _packetQueue.Reset();
        ActiveCharacter = null;
        ActiveAccount = null;
    }

    public void SendPacket(byte[] packet) {
        if(_closed == 1) return;

        if(!_packetQueue.Enqueue(packet)) {
            _log.Warn($"[Session {Id}] Packet queue is full, dropping packet.");
            return;
        }

        if(Interlocked.CompareExchange(ref _processing, 1, 0) == 0) {
            Task.Run(ProcessPackets);
        }
    }

    private void ProcessPackets() {
        try {
            byte[] buffer = new byte[8192];

            while(_packetQueue.Count > 0) {
                if(_closed == 1) return;

                int bytesRead = _packetQueue.Dequeue(buffer);
                if(bytesRead == 0) return;

                _writeEventArg.SetBuffer(buffer, 0, bytesRead);

                try {
                    if(Interlocked.Exchange(ref _sending, 1) == 0) {
                        var willRaise = Socket.SendAsync(_writeEventArg);
                        if(!willRaise)
                            ProcessSend(_writeEventArg);
                    }
                    else {
                        Interlocked.Exchange(ref _sending, 0);
                    }
                }
                catch(ObjectDisposedException) {
                    return;
                }
            }
        }
        finally {
            Interlocked.Exchange(ref _processing, 0);
        }
    }

    private void WriteComplete(object sender, SocketAsyncEventArgs e) {
        if(e.LastOperation == SocketAsyncOperation.Send) {
            ProcessSend(e);
        }
        else {
            throw new ArgumentException("The last operation completed on the socket was not a send.");
        }
    }

    private void ProcessSend(SocketAsyncEventArgs e) {
        if(e.SocketError == SocketError.Success) {
            _network.OnSend(this, e.Buffer, e.Offset, e.BytesTransferred);
        }
        else {
            _log.Error($"Error on ProcessSend: {e.SocketError}");
            Close();
        }

        Interlocked.Exchange(ref _sending, 0);
    }

    public void Close() {
        if(Interlocked.Exchange(ref _closed, 1) == 1)
            return;

        _packetQueue.Reset();
        _network.OnDisconnect(this);

        try {
            Socket.Shutdown(SocketShutdown.Receive);
        }
        catch(Exception) {
            // ignored
        }

        Socket.Close();
        _network.RemoveSession(this);

        SessionHandler.Instance.ReturnSocketEvent(ReadEventArg);
    }

    public void Dispose() {
        _writeEventArg?.Dispose();
        GC.SuppressFinalize(this);
    }
}