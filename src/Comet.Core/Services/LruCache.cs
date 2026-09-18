namespace Comet.Core.Services;

public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly long _maxWeight;
    private readonly Func<TValue, long>? _weightSelector;
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _map = new();
    private readonly LinkedList<Entry> _lru = new();
    private readonly object _gate = new();
    private long _currentWeight;

    public LruCache(int capacity, long maxWeight = long.MaxValue, Func<TValue, long>? weightSelector = null)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (maxWeight <= 0) throw new ArgumentOutOfRangeException(nameof(maxWeight));
        _capacity = capacity;
        _maxWeight = maxWeight;
        _weightSelector = weightSelector;
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
        var weight = GetWeight(value);

        lock (_gate)
        {
            if (_map.TryGetValue(key, out var existing))
            {
                _currentWeight -= existing.Value.Weight;
                existing.Value = new Entry(key, value, weight);
                _currentWeight += weight;
                _lru.Remove(existing);
                _lru.AddFirst(existing);
            }
            else
            {
                var node = _lru.AddFirst(new Entry(key, value, weight));
                _map[key] = node;
                _currentWeight += weight;
            }

            // Keep at least the most-recent item even when one unusually large
            // decoded page exceeds the budget, avoiding immediate re-decode loops.
            while (_map.Count > 1 && (_map.Count > _capacity || _currentWeight > _maxWeight))
                RemoveLeastRecentlyUsed();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _map.Clear();
            _lru.Clear();
            _currentWeight = 0;
        }
    }

    private long GetWeight(TValue value)
    {
        if (_weightSelector is null)
            return 1;

        try
        {
            return Math.Max(1, _weightSelector(value));
        }
        catch
        {
            return 1;
        }
    }

    private void RemoveLeastRecentlyUsed()
    {
        var last = _lru.Last;
        if (last is null) return;

        _lru.RemoveLast();
        _map.Remove(last.Value.Key);
        _currentWeight -= last.Value.Weight;
    }

    private readonly record struct Entry(TKey Key, TValue Value, long Weight);
}
