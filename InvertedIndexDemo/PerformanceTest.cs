using System.Diagnostics;

namespace InvertedIndexDemo
{
    public static class PerformanceTest
    {
        private static readonly string[] Vocabulary =
        {
            "cloud", "database", "server", "compiler", "runtime", "framework", "library",
            "algorithm", "network", "security", "testing", "interface", "generic", "lambda",
            "thread", "memory", "cache", "query", "index", "search", "vector", "matrix",
            "parser", "token", "stream", "socket", "kernel", "driver", "plugin", "module"
        };

        /// <summary>
        /// Генерує count документів: один з 5 базових текстів + 6 випадкових слів зі словника.
        /// Random з фіксованим seed - результати відтворювані.
        /// </summary>
        public static List<Document> GenerateDocuments(List<Document> baseDocuments, int count)
        {
            var random = new Random(42);
            var result = new List<Document>(count);

            for (int i = 0; i < count; i++)
            {
                Document template = baseDocuments[i % baseDocuments.Count];
                var extra = Enumerable.Range(0, 6).Select(_ => Vocabulary[random.Next(Vocabulary.Length)]);
                result.Add(new Document
                {
                    Id = i + 1,
                    Title = $"{template.Title} #{i + 1}",
                    Content = template.Content + " " + string.Join(" ", extra)
                });
            }

            return result;
        }

        /// <summary>
        /// Порівнює лінійний пошук та пошук через індекс для одного слова.
        /// Обидва підходи використовують ОДНАКОВУ нормалізацію - TextProcessor.Tokenize.
        /// Кожен пошук повторюється repeats разів, береться середній час.
        /// </summary>
        public static BenchmarkResult CompareSearchMethods(List<Document> documents, string searchWord, int repeats = 50)
        {
            string normalizedWord = TextProcessor.Normalize(searchWord);
            var sw = new Stopwatch();

            // 1. Лінійний пошук (без індексу)
            List<Document> linearResults = LinearSearch(documents, normalizedWord); // прогрів (JIT)
            sw.Start();
            for (int i = 0; i < repeats; i++)
            {
                linearResults = LinearSearch(documents, normalizedWord);
            }
            sw.Stop();
            double linearMicro = sw.Elapsed.TotalMilliseconds * 1000 / repeats;

            // 2. Inverted Index (побудова вимірюється окремо)
            sw.Restart();
            var index = new InvertedIndex();
            foreach (var doc in documents)
            {
                index.AddDocument(doc);
            }
            sw.Stop();
            double buildMs = sw.Elapsed.TotalMilliseconds;

            List<Document> indexResults = index.SearchSingleWord(searchWord); // прогрів
            sw.Restart();
            for (int i = 0; i < repeats; i++)
            {
                indexResults = index.SearchSingleWord(searchWord);
            }
            sw.Stop();
            double indexMicro = sw.Elapsed.TotalMilliseconds * 1000 / repeats;

            return new BenchmarkResult
            {
                DocumentCount = documents.Count,
                Word = normalizedWord,
                LinearMicroseconds = linearMicro,
                IndexMicroseconds = indexMicro,
                IndexBuildMilliseconds = buildMs,
                LinearFound = linearResults.Count,
                IndexFound = indexResults.Count
            };
        }

        private static List<Document> LinearSearch(List<Document> documents, string normalizedWord)
        {
            return documents.Where(d => TextProcessor.Tokenize(d.Content).Contains(normalizedWord)).ToList();
        }

        public static void PrintTable(IEnumerable<BenchmarkResult> results)
        {
            Console.WriteLine("| Документів | Слово       | Лінійний, мкс | Індекс, мкс | Прискорення | Побудова індексу, мс | Знайдено (лін/інд) |");
            Console.WriteLine("|-----------:|-------------|--------------:|------------:|------------:|---------------------:|:------------------:|");
            foreach (var r in results)
            {
                Console.WriteLine($"| {r.DocumentCount,10} | {r.Word,-11} | {r.LinearMicroseconds,13:F1} | {r.IndexMicroseconds,11:F1} | {r.Speedup,10:F0}x | {r.IndexBuildMilliseconds,20:F2} | {r.LinearFound,8} / {r.IndexFound,-7} |");
            }
        }
    }

    public class BenchmarkResult
    {
        public int DocumentCount { get; set; }
        public string Word { get; set; }
        public double LinearMicroseconds { get; set; }
        public double IndexMicroseconds { get; set; }
        public double IndexBuildMilliseconds { get; set; }
        public int LinearFound { get; set; }
        public int IndexFound { get; set; }
        public double Speedup => IndexMicroseconds > 0 ? LinearMicroseconds / IndexMicroseconds : 0;
    }
}
