using System.Globalization;
using System.Text;

namespace InvertedIndexDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA"); // українське форматування чисел і дат
            Console.InputEncoding = Encoding.UTF8;

            var documents = CreateDocuments();
            var index = new InvertedIndex();
            LoadDocuments(index, documents);

            Console.WriteLine($"Loaded {index.DocumentCount} documents");
            Console.WriteLine($"Index contains {index.UniqueWords} unique words");
            Console.WriteLine();

            RunDemo(index);
            RunBenchmark(documents);

            // Режим без інтерактиву (для автоматичного запуску): dotnet run -- --no-interactive
            if (args.Contains("--no-interactive"))
            {
                return;
            }

            InteractiveSearch(index);
        }

        private static List<Document> CreateDocuments()
        {
            return new List<Document>
            {
                new Document
                {
                    Id = 1,
                    Title = "Introduction to C#",
                    Content = "C# is a modern object-oriented programming language developed by Microsoft."
                },
                new Document
                {
                    Id = 2,
                    Title = "Java Basics",
                    Content = "Java is a popular programming language known for its portability and performance."
                },
                new Document
                {
                    Id = 3,
                    Title = "Python Overview",
                    Content = "Python is a high-level programming language with simple syntax."
                },
                new Document
                {
                    Id = 4,
                    Title = "C# Advanced Features",
                    Content = "C# supports LINQ, async/await, and many modern programming paradigms."
                },
                new Document
                {
                    Id = 5,
                    Title = "Comparing Languages",
                    Content = "C#, Java, and Python are among the most popular programming languages today."
                }
            };
        }

        private static void LoadDocuments(InvertedIndex index, List<Document> documents)
        {
            foreach (var doc in documents)
            {
                index.AddDocument(doc);
            }
        }

        private static void RunDemo(InvertedIndex index)
        {
            Console.WriteLine("=== Демонстрація методів пошуку ===");
            Print("SearchSingleWord(\"python\")", index.SearchSingleWord("python"));
            Print("SearchMultipleWords(\"programming\", \"language\")  [AND]", index.SearchMultipleWords("programming", "language"));
            Print("SearchMultipleWords(\"c#\", \"async\")  [AND]", index.SearchMultipleWords("c#", "async"));
            Print("SearchAnyWord(\"java\", \"python\")  [OR]", index.SearchAnyWord("java", "python"));
            Print("SearchPhrase(\"programming language\")  (*)", index.SearchPhrase("programming language"));
            Print("SearchPhrase(\"language programming\")  (*)", index.SearchPhrase("language programming"));

            Console.WriteLine("SearchRanked(\"c#\", \"modern\")  (*) TF-IDF:");
            foreach (var (doc, score) in index.SearchRanked("c#", "modern"))
            {
                Console.WriteLine($"  {doc}  score = {score:F3}");
            }

            Console.WriteLine("Стоп-слова: " + string.Join(" ", TextProcessor.RemoveStopWords(TextProcessor.Tokenize("C# is a language for the web"))));
            Console.WriteLine();
        }

        private static void Print(string title, List<Document> docs)
        {
            Console.WriteLine($"{title}: {(docs.Count == 0 ? "нічого не знайдено" : string.Join(", ", docs.Select(d => d.Id)))}");
        }

        private static void RunBenchmark(List<Document> baseDocuments)
        {
            Console.WriteLine("=== Вимірювання продуктивності (середнє з 50 пошуків) ===");
            var results = new List<BenchmarkResult>();

            foreach (int count in new[] { 10, 100, 1000, 10000 })
            {
                var docs = PerformanceTest.GenerateDocuments(baseDocuments, count);
                results.Add(PerformanceTest.CompareSearchMethods(docs, "programming")); // часте слово
                results.Add(PerformanceTest.CompareSearchMethods(docs, "Microsoft"));   // рідкісне слово
            }

            PerformanceTest.PrintTable(results);

            bool allMatch = results.All(r => r.LinearFound == r.IndexFound);
            Console.WriteLine($"Кількість знайдених документів збігається в усіх тестах: {(allMatch ? "так" : "НІ")}");
            Console.WriteLine();
        }

        private static void InteractiveSearch(InvertedIndex index)
        {
            while (true)
            {
                Console.Write("Enter search query (or 'exit'): ");
                var query = Console.ReadLine();

                if (query == null || query.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                {
                    continue;
                }

                Console.WriteLine("\n--- Search Results (AND) ---");
                var results = index.SearchMultipleWords(words);
                foreach (var doc in results)
                {
                    Console.WriteLine(doc);
                    string snippet = doc.Content.Substring(0, Math.Min(100, doc.Content.Length));
                    Console.WriteLine($"  {InvertedIndex.Highlight(snippet, words)}...");
                }

                Console.WriteLine($"\nFound {results.Count} documents\n");
            }
        }
    }
}
