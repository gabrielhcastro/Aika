namespace GameServer.Core.Handlers;
public class RingBuffer(int capacity) {
    private readonly byte[] _buffer = new byte[capacity];
    private readonly int _capacity = capacity;
    private int _head;
    private int _tail;
    private int _size;
    private readonly object _lock = new();

    public bool Enqueue(byte[] data) {
        lock(_lock) {
            if(data.Length > _capacity - _size)
                return false;

            foreach(byte b in data) {
                _buffer[_tail] = b;
                _tail = (_tail + 1) % _capacity;
            }

            _size += data.Length;
            return true;
        }
    }

    public int Dequeue(byte[] outputBuffer) {
        lock(_lock) {
            if(_size == 0)
                return 0;

            int bytesToRead = Math.Min(outputBuffer.Length, _size);

            for(int i = 0; i < bytesToRead; i++) {
                outputBuffer[i] = _buffer[_head];
                _head = (_head + 1) % _capacity;
            }

            _size -= bytesToRead;
            return bytesToRead;
        }
    }

    public void Reset() {
        lock(_lock) {
            _head = 0;
            _tail = 0;
            _size = 0;
        }
    }

    public bool IsEmpty => _size == 0;
    public int Count => _size;
}