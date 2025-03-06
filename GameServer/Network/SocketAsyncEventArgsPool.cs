using Shared.Core;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace GameServer.Network;
public class SocketAsyncEventArgsPool : Singleton<SocketAsyncEventArgsPool> {
    private readonly ConcurrentBag<SocketAsyncEventArgs> _pool = [];
    private readonly int _initialSize;
    private readonly EventHandler<SocketAsyncEventArgs> _completedHandler;

    public SocketAsyncEventArgsPool() { }

    public SocketAsyncEventArgsPool(int initialSize, EventHandler<SocketAsyncEventArgs> completedHandler) {
        _initialSize = initialSize;
        _completedHandler = completedHandler;

        for(int i = 0; i < _initialSize; i++) {
            var args = CreateNew();
            _pool.Add(args);
        }
    }

    private SocketAsyncEventArgs CreateNew() {
        var args = new SocketAsyncEventArgs();
        args.Completed += _completedHandler;
        return args;
    }

    public SocketAsyncEventArgs Rent() {
        if(_pool.TryTake(out var args)) {
            return args;
        }

        return CreateNew();
    }

    public void Return(SocketAsyncEventArgs args) {
        if(args == null) return;

        args.AcceptSocket = null;
        args.UserToken = null;
        args.SetBuffer(null, 0, 0);

        _pool.Add(args);
    }
}
