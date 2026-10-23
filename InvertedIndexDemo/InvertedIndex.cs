namespace InvertedIndexDemo
{
    public class InvertedIndex
    {
        // Слово -> Множина ID документів
        private readonly Dictionary<string, HashSet<int>> _index;

        // ID -> Документ (для швидкого отримання)
        private readonly Dictionary<int, Document> _documents;

        // Слово -> (ID документа -> кількість входжень), для ранжування
        private readonly Dictionary<string, Dictionary<int, int>> _termFrequency;

        public InvertedIndex()
        {
            _index = new Dictionary<string, HashSet<int>>();
            _documents = new Dictionary<int, Document>();
            _termFrequency = new Dictionary<string, Dictionary<int, int>>();
        }

        /// <summary>
        /// Додає документ до індексу.
        /// </summary>
        public void AddDocument(Document doc)
        {
            // 1. Зберегти документ
            _documents[doc.Id] = doc;

            // 2. Розбити Content на слова (нижній регістр, без пунктуації)
            foreach (string word in TextProcessor.Tokenize(doc.Content))
            {
                // 3. Додати doc.Id до _index[word]
                if (!_index.TryGetValue(word, out HashSet<int> ids))
                {
                    ids = new HashSet<int>();
                    _index[word] = ids;
                }
                ids.Add(doc.Id);

                if (!_termFrequency.TryGetValue(word, out Dictionary<int, int> freq))
                {
                    freq = new Dictionary<int, int>();
                    _termFrequency[word] = freq;
                }
                freq[doc.Id] = freq.GetValueOrDefault(doc.Id) + 1;
            }
        }

        /// <summary>
        /// Шукає документи, що містять слово.
        /// </summary>
        public List<Document> SearchSingleWord(string word)
        {
            string normalized = TextProcessor.Normalize(word);
            if (!_index.TryGetValue(normalized, out HashSet<int> ids))
            {
                return new List<Document>();
            }
            return ToDocuments(ids);
        }

        /// <summary>
        /// Шукає документи, що містять ВСІ слова (AND) - HashSet.IntersectWith.
        /// </summary>
        public List<Document> SearchMultipleWords(params string[] words)
        {
            HashSet<int> result = null;

            foreach (string raw in words)
            {
                string word = TextProcessor.Normalize(raw);
                if (word.Length == 0)
                {
                    continue;
                }

                if (!_index.TryGetValue(word, out HashSet<int> ids))
                {
                    return new List<Document>(); // одне слово відсутнє -> AND порожній
                }

                if (result == null)
                {
                    result = new HashSet<int>(ids); // копія, щоб не зіпсувати індекс
                }
                else
                {
                    result.IntersectWith(ids);
                }

                if (result.Count == 0)
                {
                    break;
                }
            }

            return result == null ? new List<Document>() : ToDocuments(result);
        }

        /// <summary>
        /// Шукає документи, що містять БУДЬ-ЯКЕ слово (OR) - HashSet.UnionWith.
        /// </summary>
        public List<Document> SearchAnyWord(params string[] words)
        {
            var result = new HashSet<int>();
            foreach (string raw in words)
            {
                if (_index.TryGetValue(TextProcessor.Normalize(raw), out HashSet<int> ids))
                {
                    result.UnionWith(ids);
                }
            }
            return ToDocuments(result);
        }

        /// <summary>
        /// (*) Ранжування: AND-пошук, відсортований за TF-IDF.
        /// </summary>
        public List<(Document Doc, double Score)> SearchRanked(params string[] words)
        {
            List<Document> found = SearchMultipleWords(words);
            var normalized = words.Select(TextProcessor.Normalize).Where(w => w.Length > 0).Distinct().ToList();

            return found
                .Select(doc => (Doc: doc, Score: normalized.Sum(w => TfIdf(w, doc.Id))))
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Doc.Id)
                .ToList();
        }

        /// <summary>
        /// (*) Пошук точної фрази: спочатку AND через індекс, потім перевірка
        /// послідовності токенів тільки в знайдених документах.
        /// </summary>
        public List<Document> SearchPhrase(string phrase)
        {
            var phraseTokens = TextProcessor.Tokenize(phrase).ToArray();
            if (phraseTokens.Length == 0)
            {
                return new List<Document>();
            }

            return SearchMultipleWords(phraseTokens)
                .Where(doc => ContainsSequence(TextProcessor.Tokenize(doc.Content).ToArray(), phraseTokens))
                .ToList();
        }

        /// <summary>
        /// (*) Підсвітка знайдених слів у тексті: слово -> [слово].
        /// </summary>
        public static string Highlight(string content, IEnumerable<string> words)
        {
            var targets = new HashSet<string>(words.Select(TextProcessor.Normalize));
            return System.Text.RegularExpressions.Regex.Replace(
                content,
                @"[\w#+]+",
                m => targets.Contains(m.Value.ToLowerInvariant()) ? $"[{m.Value}]" : m.Value);
        }

        public int DocumentCount => _documents.Count;
        public int UniqueWords => _index.Keys.Count;

        private double TfIdf(string word, int docId)
        {
            if (!_termFrequency.TryGetValue(word, out Dictionary<int, int> freq) || !freq.ContainsKey(docId))
            {
                return 0;
            }
            double tf = freq[docId];
            double idf = Math.Log((double)_documents.Count / freq.Count) + 1;
            return tf * idf;
        }

        private static bool ContainsSequence(string[] tokens, string[] phrase)
        {
            for (int i = 0; i + phrase.Length <= tokens.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < phrase.Length; j++)
                {
                    if (tokens[i + j] != phrase[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return true;
                }
            }
            return false;
        }

        private List<Document> ToDocuments(IEnumerable<int> ids)
        {
            return ids.OrderBy(id => id).Select(id => _documents[id]).ToList();
        }
    }
}
