using GameServer.Model.Account;
using GameServer.Model.Character;
using NLog;
using Shared.Models.Account;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GameServer.Core.Base;

public class Session : IDisposable {
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    private readonly INetwork _network;
    private readonly Dictionary<string, object> _attributes = [];
    private readonly SocketAsyncEventArgs _writeEventArg = new();
    private ConcurrentQueue<byte[]> _packetQueue = new();
    private int _processing;
    private int _sending;
    private int _closed;
    private IPEndPoint RemoteEndPoint => (IPEndPoint)Socket.RemoteEndPoint;
    public uint Id { get; }
    public Socket Socket { get; }
    public SocketAsyncEventArgs ReadEventArg { get; }
    public IPAddress Ip { get; }
    public DateTime LastActivity { get; set; }
    public string Username { get; set; }
    public CharacterEntitie ActiveCharacter { get; set; }
    public AccountEntitie ActiveAccount { get; set; }

    public Session(INetwork network, SocketAsyncEventArgs readEventArg, Socket socket) {
        Socket = socket;
        Id = (uint)RemoteEndPoint.GetHashCode();
        _network = network;
        ReadEventArg = readEventArg;
        _writeEventArg.Completed += WriteComplete;
        Ip = RemoteEndPoint.Address;
    }

    public void SendPacket(byte[] packet) {
        if(_closed == 1) return;

        _packetQueue.Enqueue(packet);

        if(Interlocked.CompareExchange(ref _processing, 1, 0) == 0) {
            Task.Run(ProcessPackets);
        }
    }

    private void ProcessPackets() {
        try {
            while(_packetQueue.TryDequeue(out var buffer)) {
                if(_closed == 1) return;

                _writeEventArg.SetBuffer(buffer, 0, buffer.Length);

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

        _packetQueue.Clear();
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