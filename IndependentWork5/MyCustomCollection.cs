using System.Collections;

namespace IndependentWork5
{
    /// <summary>
    /// Власна колекція рядків. Реалізує IEnumerable&lt;string&gt;,
    /// тому її можна перебирати через foreach і використовувати з LINQ.
    /// </summary>
    public class MyCustomCollection : IEnumerable<string>
    {
        private readonly List<string> _items = new List<string>();

        public void Add(string item)
        {
            _items.Add(item);
        }

        public int Count => _items.Count;

        // Узагальнена версія - її використовує foreach
        public IEnumerator<string> GetEnumerator()
        {
            // yield return - компілятор сам генерує клас-перечислювач
            foreach (string item in _items)
            {
                yield return item;
            }
        }

        // Неузагальнена версія з IEnumerable - явна реалізація, делегує узагальненій
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
