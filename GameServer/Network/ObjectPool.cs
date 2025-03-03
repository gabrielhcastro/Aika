using System.Collections.Concurrent;

namespace GameServer.Network;
public class ObjectPool<T> where T : class, new() {
    private readonly ConcurrentBag<T> _pool = new();

    public T Rent() {
        return _pool.TryTake(out var item) ? item : new T();
    }

    public void Return(T item) {
        _pool.Add(item);
    }
}