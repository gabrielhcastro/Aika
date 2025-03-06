using GameServer.Core.Base;
using GameServer.Core.Handlers;
using NLog;
using System.Net;
using System.Net.Sockets;

namespace GameServer.Network;

public class Server : INetwork {
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    private const int ReceiveBufferSize = 8096;
    private readonly BufferHandler _bufferControl;
    private readonly Socket _listenSocket;
    private readonly Semaphore _maxNumberAcceptedClients;
    private readonly BaseProtocol _protocol;
    public bool IsStarted { get; private set; }
    public string Name { get; set; }
    public byte NationId { get; set; }

    public Server(EndPoint localEndPoint, int numConnections, BaseProtocol handler) {
        _listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(localEndPoint);
        _listenSocket.Listen(100);

        _bufferControl = new BufferHandler(ReceiveBufferSize * numConnections, ReceiveBufferSize);
        _maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        _bufferControl.Init();

        _protocol = handler;
    }

    public async void StartAsync() {
        IsStarted = true;
        StartAccept(null);

        await Task.WhenAll(StartPeriodicRecicle());
    }

    public async Task StartPeriodicRecicle() {
        await Task.Run(async () => {
            while(IsStarted) {
                await Task.Delay(TimeSpan.FromHours(1));

                if(SessionHandler.Instance.GetAllSessions().Count == 0) {
                    Console.WriteLine("[BufferHandler] Reciclando buffers...");
                    _bufferControl.Release();
                    _bufferControl.Init();
                }
            }
        });
    }

    public void Stop() {
        foreach(var session in SessionHandler.Instance.GetAllSessions()) {
            SessionHandler.Instance.RemoveSession(session);
        }

        IsStarted = false;
        _listenSocket.Close();
    }

    private void StartAccept(SocketAsyncEventArgs acceptEventArg) {
        if(acceptEventArg == null) {
            acceptEventArg = SocketAsyncEventArgsPool.Instance.Rent();
            acceptEventArg.Completed += AcceptEventArg_Completed;
        }
        else {
            acceptEventArg.AcceptSocket = null;
    }

        _maxNumberAcceptedClients.WaitOne();
        var willRaiseEvent = _listenSocket.AcceptAsync(acceptEventArg);
        if(!willRaiseEvent)
            ProcessAccept(acceptEventArg);
    }

    private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e) {
        ProcessAccept(e);
    }

    private void ProcessAccept(SocketAsyncEventArgs e) {
        var readEventArg = SocketAsyncEventArgsPool.Instance.Rent();
        readEventArg.Completed += ReadComplete;
        _bufferControl.Set(readEventArg);

        readEventArg.AcceptSocket = e.AcceptSocket;

        var session = SessionHandler.Instance.RentSession(this, readEventArg, e.AcceptSocket);
        readEventArg.UserToken = session;

        _protocol.OnConnect(session);

        SessionHandler.Instance.AddSession(session);

        var willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArg);
        if(!willRaiseEvent)
            ProcessReceive(readEventArg);

        StartAccept(e);
    }

    private void ReadComplete(object sender, SocketAsyncEventArgs e) {
        switch(e.LastOperation) {
            case SocketAsyncOperation.Receive:
            ProcessReceive(e);
            break;
            default:
            throw new ArgumentException("The last operation completed on the socket was not a receive");
        }
    }

    private void ProcessReceive(SocketAsyncEventArgs e) {
        var session = (Session)e.UserToken;
        if(e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {
            Span<byte> buffer = e.BytesTransferred <= 128
                ? stackalloc byte[e.BytesTransferred]  // Pacotes pequenos stackalloc
                : new byte[e.BytesTransferred]; // Pacotes grandes Heap

            e.Buffer.AsSpan(e.Offset, e.BytesTransferred).CopyTo(buffer);

            _protocol.OnReceive(session, buffer.ToArray(), e.BytesTransferred);

            try {
                var willRaiseEvent = session.Socket.ReceiveAsync(e);
                if(!willRaiseEvent)
                    ProcessReceive(e);
            }
            catch(ObjectDisposedException) {
                session.Close();
            }
        }
        else {
            if(e.SocketError != SocketError.Success && e.SocketError != SocketError.OperationAborted &&
                e.SocketError != SocketError.ConnectionReset)
                _log.Error("Error on ProcessReceive: {0}", e.SocketError.ToString());
            session.Close();
        }
    }

    public void RemoveSession(Session session) {
        _bufferControl.Empty(session.ReadEventArg);

        SocketAsyncEventArgsPool.Instance.Return(session.ReadEventArg);
        SessionHandler.Instance.RemoveSession(session);
        SessionHandler.Instance.ReturnSession(session);

        if(SessionHandler.Instance.GetAllSessions() != null)
            _maxNumberAcceptedClients.Release();
    }

    public void OnConnect(Session session) {
        _protocol.OnConnect(session);
    }

    public void OnDisconnect(Session session) {
        _protocol.OnDisconnect(session);
    }

    public void OnReceive(Session session, byte[] buff, int bytes) {
        _protocol.OnReceive(session, buff, bytes);
        SessionHandler.UpdateSessionActivity(session);
    }

    public void OnSend(Session session, byte[] buff, int offset, int bytes) {
        _protocol.OnSend(session, buff, offset, bytes);
    }
}