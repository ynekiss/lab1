using System.Globalization;
using System.Text;

namespace IndependentWork8
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA"); // українське форматування чисел і дат

            DemoFileMetadataCache();
            DemoTagManager();
            DemoSearchIndex();
            DemoCustomComparers();
        }

        private static void DemoFileMetadataCache()
        {
            Console.WriteLine("=== Сценарій 1. FileMetadataCache (Dictionary) ===");
            var cache = new FileMetadataCache();
            DateTime modified = new DateTime(2026, 10, 20, 14, 30, 0);

            cache.Add(@"C:\Docs\Report.docx", new FileMetadata { FileName = "Report.docx", Size = 48_213, LastModified = modified, Hash = "a1b2c3" });
            cache.Add(@"C:\Docs\photo.png", new FileMetadata { FileName = "photo.png", Size = 1_204_551, LastModified = modified.AddDays(-3), Hash = "ff0921" });
            cache.Add(@"C:\Docs\notes.txt", new FileMetadata { FileName = "notes.txt", Size = 812, LastModified = modified.AddHours(-5), Hash = "09cd77" });

            Console.WriteLine($"У кеші файлів: {cache.Count}");
            Console.WriteLine($"Get(\"c:\\docs\\REPORT.DOCX\") -> {cache.Get(@"c:\docs\REPORT.DOCX")}");
            Console.WriteLine($"Contains(photo.png) = {cache.Contains(@"C:\Docs\photo.png")}");
            Console.WriteLine($"Файл notes.txt змінився? {cache.IsChanged(@"C:\Docs\notes.txt", modified)}");
            Console.WriteLine($"Remove(photo.png) = {cache.Remove(@"C:\Docs\photo.png")}, тепер у кеші: {cache.Count}");
            Console.WriteLine($"Get(photo.png) -> {cache.Get(@"C:\Docs\photo.png")?.ToString() ?? "null (немає в кеші)"}");
            Console.WriteLine();
        }

        private static void DemoTagManager()
        {
            Console.WriteLine("=== Сценарій 2. TagManager (HashSet) ===");
            var tags = new TagManager();

            tags.AddTag("post-1", "csharp");
            tags.AddTag("post-1", "dotnet");
            tags.AddTag("post-1", "collections");
            tags.AddTag("post-1", "CSharp"); // дублікат (інший регістр) - не додасться

            tags.AddTag("post-2", "csharp");
            tags.AddTag("post-2", "linq");
            tags.AddTag("post-2", "collections");

            tags.AddTag("post-3", "python");
            tags.AddTag("post-3", "beginners");

            Console.WriteLine($"Теги post-1: {Format(tags.GetTags("post-1"))}");
            Console.WriteLine($"Теги post-2: {Format(tags.GetTags("post-2"))}");
            Console.WriteLine($"Усі унікальні теги (UnionWith): {Format(tags.GetAllUniqueTags())}");
            Console.WriteLine($"Спільні теги post-1 і post-2 (IntersectWith): {Format(tags.FindCommonTags("post-1", "post-2"))}");
            Console.WriteLine($"Є в post-1, але немає в post-2 (ExceptWith): {Format(tags.FindUniqueTags("post-1", "post-2"))}");
            Console.WriteLine($"Спільні теги post-1 і post-3: {Format(tags.FindCommonTags("post-1", "post-3"))}");

            tags.RemoveTag("post-1", "dotnet");
            Console.WriteLine($"Після RemoveTag(post-1, dotnet): {Format(tags.GetTags("post-1"))}");
            Console.WriteLine();
        }

        private static void DemoSearchIndex()
        {
            Console.WriteLine("=== Сценарій 3. SearchIndex (Dictionary + HashSet) ===");
            var index = new SearchIndex();
            index.AddDocument(1, "C# is a great programming language");
            index.AddDocument(2, "Java is also a programming language");
            index.AddDocument(3, "C# and Java are popular");

            Console.WriteLine($"Унікальних слів в індексі: {index.WordCount}");
            Console.WriteLine($"Search(\"c#\") -> [{string.Join(", ", index.Search("c#").OrderBy(x => x))}]");
            Console.WriteLine($"Search(\"programming\") -> [{string.Join(", ", index.Search("programming").OrderBy(x => x))}]");
            Console.WriteLine($"Search(\"JAVA\") -> [{string.Join(", ", index.Search("JAVA").OrderBy(x => x))}]");
            Console.WriteLine($"SearchMultiple(\"c#\", \"java\") -> [{string.Join(", ", index.SearchMultiple("c#", "java").OrderBy(x => x))}]");
            Console.WriteLine($"SearchMultiple(\"programming\", \"language\") -> [{string.Join(", ", index.SearchMultiple("programming", "language").OrderBy(x => x))}]");
            Console.WriteLine($"Search(\"rust\") -> [{string.Join(", ", index.Search("rust"))}] (нічого не знайдено)");
            Console.WriteLine();
        }

        private static void DemoCustomComparers()
        {
            Console.WriteLine("=== Бонус. Custom comparer ===");

            var caseInsensitive = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            caseInsensitive["Hello"] = 1;
            Console.WriteLine($"caseInsensitive[\"HELLO\"] = {caseInsensitive["HELLO"]}");

            var people = new HashSet<Person>(new PersonNameComparer());
            bool first = people.Add(new Person { Name = "John", Age = 25 });
            bool second = people.Add(new Person { Name = "John", Age = 30 });
            Console.WriteLine($"Add(John, 25) = {first}, Add(John, 30) = {second}, Count = {people.Count}");
        }

        private static string Format(HashSet<string> set) =>
            set.Count == 0 ? "(порожньо)" : "{" + string.Join(", ", set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)) + "}";
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class PersonNameComparer : IEqualityComparer<Person>
    {
        public bool Equals(Person x, Person y) => x?.Name == y?.Name;

        public int GetHashCode(Person obj) => obj.Name?.GetHashCode() ?? 0;
    }
}
