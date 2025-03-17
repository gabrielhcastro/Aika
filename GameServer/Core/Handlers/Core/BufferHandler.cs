using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace GameServer.Core.Handlers.Core;
public class BufferHandler(int totalBufferSize, int blockSize) {
    private readonly int _bufferSize = totalBufferSize;
    private readonly int _blockSize = blockSize;
    private readonly ConcurrentStack<int> _freeIndexPool = new();
    private int _posIndex = 0;
    private byte[] _buffer;

    public void Init() {
        _buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
    }

    public void Empty(SocketAsyncEventArgs args) {
        _freeIndexPool.Push(args.Offset);
    }

    public void Set(SocketAsyncEventArgs args) {
        if(_freeIndexPool.TryPop(out var offset)) args.SetBuffer(_buffer, offset, _blockSize);
        else {
            int newPos = Interlocked.Add(ref _posIndex, _blockSize);

            if(newPos < _bufferSize) args.SetBuffer(_buffer, newPos, _blockSize);
            else {
                args.SetBuffer(ArrayPool<byte>.Shared.Rent(_blockSize), 0, _blockSize);
            }
        }
    }

    public void Release() {
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}