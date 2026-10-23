namespace IndependentWork7
{
    /// <summary>
    /// Узагальнений кеш з обмеженою ємністю та політикою витіснення FIFO
    /// (First In, First Out): коли кеш повний, видаляється елемент,
    /// який було додано найраніше.
    /// </summary>
    public class Cache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, TValue> _cache;
        private readonly Queue<TKey> _keysOrder;

        public Cache(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Ємність кешу має бути більшою за 0.");
            }

            _capacity = capacity;
            _cache = new Dictionary<TKey, TValue>(capacity);
            _keysOrder = new Queue<TKey>(capacity);
        }

        /// <summary>Поточна кількість елементів у кеші.</summary>
        public int Count => _cache.Count;

        /// <summary>Максимальна кількість елементів.</summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Додає елемент. Якщо ключ уже є - оновлює значення (порядок FIFO не змінюється).
        /// Якщо кеш повний - спочатку видаляє найстаріший елемент.
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            if (_cache.ContainsKey(key))
            {
                _cache[key] = value;
                return;
            }

            if (_cache.Count >= _capacity)
            {
                TKey oldestKey = _keysOrder.Dequeue();
                _cache.Remove(oldestKey);
            }

            _cache.Add(key, value);
            _keysOrder.Enqueue(key);
        }

        /// <summary>
        /// Повертає значення за ключем або викидає KeyNotFoundException.
        /// </summary>
        public TValue Get(TKey key)
        {
            if (_cache.TryGetValue(key, out TValue value))
            {
                return value;
            }

            throw new KeyNotFoundException($"Ключ '{key}' відсутній у кеші.");
        }

        public bool ContainsKey(TKey key)
        {
            return _cache.ContainsKey(key);
        }

        /// <summary>Ключі в порядку додавання (від найстарішого).</summary>
        public IEnumerable<TKey> Keys => _keysOrder.ToArray();
    }
}
