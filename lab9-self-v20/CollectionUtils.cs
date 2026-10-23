namespace lab9_self_v20
{
    /// <summary>
    /// Власні узагальнені утиліти для колекцій.
    /// Поведінка передається у методи через делегати (Func, Predicate, Action).
    /// </summary>
    public static class CollectionUtils
    {
        /// <summary>
        /// Перетворює кожен елемент за допомогою mapper. Аналог LINQ Select().
        /// </summary>
        public static IEnumerable<TResult> Map<TSource, TResult>(
            IEnumerable<TSource> collection,
            Func<TSource, TResult> mapper)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(mapper);

            var result = new List<TResult>();
            foreach (var item in collection)
            {
                result.Add(mapper(item));
            }
            return result;
        }

        /// <summary>
        /// Знаходить елемент з максимальним значенням ключа.
        /// Для порожньої колекції повертає default(T).
        /// Якщо максимум у кількох елементів - повертає перший з них.
        /// </summary>
        public static T MaxBy<T, TKey>(
            IEnumerable<T> collection,
            Func<T, TKey> keySelector)
            where TKey : IComparable<TKey>
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(keySelector);

            T maxItem = default(T);
            TKey maxKey = default(TKey);
            bool first = true;

            foreach (var item in collection)
            {
                TKey key = keySelector(item);
                if (first || key.CompareTo(maxKey) > 0)
                {
                    maxKey = key;
                    maxItem = item;
                    first = false;
                }
            }

            return maxItem;
        }

        /// <summary>
        /// Повертає елементи, що задовольняють умову. Аналог LINQ Where().
        /// </summary>
        public static IEnumerable<T> Filter<T>(
            IEnumerable<T> collection,
            Predicate<T> predicate)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(predicate);

            var result = new List<T>();
            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        /// <summary>
        /// Виконує дію для кожного елемента.
        /// </summary>
        public static void ForEach<T>(
            IEnumerable<T> collection,
            Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(action);

            foreach (var item in collection)
            {
                action(item);
            }
        }
    }
}
