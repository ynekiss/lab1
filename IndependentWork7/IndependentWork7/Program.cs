using System.Text;

namespace IndependentWork7
{
    public class Student
    {
        public string Name { get; set; }
        public double AverageGrade { get; set; }

        public override string ToString() => $"{Name} (середній бал {AverageGrade:F1})";
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("=== Cache<string, string> (ємність 3) ===");
            Cache<string, string> translations = new Cache<string, string>(3);

            translations.Add("cat", "кіт");
            translations.Add("dog", "собака");
            translations.Add("bird", "птах");
            PrintState(translations);

            Console.WriteLine($"Get(\"dog\") = {translations.Get("dog")}");

            Console.WriteLine("Додаємо \"fish\" - кеш повний, витісняється найстаріший (\"cat\")");
            translations.Add("fish", "риба");
            PrintState(translations);
            Console.WriteLine($"ContainsKey(\"cat\") = {translations.ContainsKey("cat")}");
            Console.WriteLine($"ContainsKey(\"fish\") = {translations.ContainsKey("fish")}");

            try
            {
                translations.Get("cat");
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"Get(\"cat\") -> KeyNotFoundException: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("=== Cache<int, Student> (ємність 2) ===");
            Cache<int, Student> students = new Cache<int, Student>(2);
            students.Add(1, new Student { Name = "Іван", AverageGrade = 4.5 });
            students.Add(2, new Student { Name = "Марія", AverageGrade = 4.9 });
            students.Add(3, new Student { Name = "Олег", AverageGrade = 3.8 });
            Console.WriteLine($"Count = {students.Count}, ключі: {string.Join(", ", students.Keys)}");
            Console.WriteLine($"Get(2) = {students.Get(2)}");
            Console.WriteLine($"Get(3) = {students.Get(3)}");
            Console.WriteLine($"ContainsKey(1) = {students.ContainsKey(1)} (витіснено як найстаріший)");
        }

        private static void PrintState<TKey, TValue>(Cache<TKey, TValue> cache)
        {
            Console.WriteLine($"  Count = {cache.Count}/{cache.Capacity}, ключі (від найстарішого): {string.Join(", ", cache.Keys)}");
        }
    }
}
