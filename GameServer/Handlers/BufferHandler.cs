using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace GameServer.Handlers;

public class BufferHandler {
    private readonly int _bufferSize;
    private readonly int _blockSize;
    private readonly ConcurrentStack<int> _freeIndexPool;
    private int _posIndex;
    private byte[] _buffer;

    public BufferHandler(int totalBufferSize, int blockSize) {
        _bufferSize = totalBufferSize;
        _blockSize = blockSize;
        _freeIndexPool = new ConcurrentStack<int>();
        _posIndex = 0;
    }

    public void Init() {
        // Usa pool
        _buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);

        //Aloca dinamicamente
        //_buffer = new byte[_byteLenght];
    }

    public void Empty(SocketAsyncEventArgs args) {
        _freeIndexPool.Push(args.Offset);
    }

    public void Set(SocketAsyncEventArgs args) {
        if(_freeIndexPool.TryPop(out var offset)) {
            args.SetBuffer(_buffer, offset, _blockSize);
        }
        else {
            int newPos = Interlocked.Add(ref _posIndex, _blockSize);

            if(newPos < _bufferSize) {
                args.SetBuffer(_buffer, newPos, _blockSize);
            }
            else {
                // Buffer cheio, usa um buffer temporário (evita travamento)
                args.SetBuffer(ArrayPool<byte>.Shared.Rent(_blockSize), 0, _blockSize);
            }
        }
    }

    public void Release() {
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}
