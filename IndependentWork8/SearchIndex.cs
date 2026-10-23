using System.Text.RegularExpressions;

namespace IndependentWork8
{
    /// <summary>
    /// Сценарій 3: простий інвертований індекс.
    /// Dictionary дає O(1) доступ до слова, HashSet - унікальні ID документів
    /// і швидкий перетин множин.
    /// </summary>
    public class SearchIndex
    {
        // Слово -> множина документів, де воно зустрічається
        private readonly Dictionary<string, HashSet<int>> _invertedIndex =
            new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

        public int WordCount => _invertedIndex.Count;

        public void AddDocument(int docId, string content)
        {
            foreach (Match match in Regex.Matches(content.ToLowerInvariant(), @"[\w#+]+"))
            {
                string word = match.Value;
                if (!_invertedIndex.TryGetValue(word, out HashSet<int> docs))
                {
                    docs = new HashSet<int>();
                    _invertedIndex[word] = docs;
                }
                docs.Add(docId); // повторне слово в тому ж документі не дублюється
            }
        }

        public HashSet<int> Search(string word)
        {
            return _invertedIndex.TryGetValue(word, out HashSet<int> docs)
                ? new HashSet<int>(docs)
                : new HashSet<int>();
        }

        /// <summary>Документи, що містять ВСІ слова (перетин множин).</summary>
        public HashSet<int> SearchMultiple(params string[] words)
        {
            if (words.Length == 0)
            {
                return new HashSet<int>();
            }

            HashSet<int> result = Search(words[0]);
            for (int i = 1; i < words.Length && result.Count > 0; i++)
            {
                result.IntersectWith(Search(words[i]));
            }
            return result;
        }
    }
}
