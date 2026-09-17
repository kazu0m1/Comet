namespace Comet.Core.Services;

public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _map = new();
    private readonly LinkedList<(TKey Key, TValue Value)> _lru = new();
    private readonly object _gate = new();

    public LruCache(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        lock (_gate)
        {
            if (!_map.TryGetValue(key, out var node))
            {
                value = default;
                return false;
            }
            _lru.Remove(node);
            _lru.AddFirst(node);
            value = node.Value.Value;
            return true;
        }
    }

    public void Set(TKey key, TValue value)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out var existing))
            {
                existing.Value = (key, value);
                _lru.Remove(existing);
                _lru.AddFirst(existing);
                return;
            }

            var node = _lru.AddFirst((key, value));
            _map[key] = node;
            if (_map.Count <= _capacity) return;

            var last = _lru.Last;
            if (last is null) return;
            _lru.RemoveLast();
            _map.Remove(last.Value.Key);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _map.Clear();
            _lru.Clear();
        }
    }
}
