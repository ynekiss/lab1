using System.Globalization;
using System.Text;

namespace lab9_self_v20
{
    public class Product
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA"); // українське форматування чисел і дат

            // ---------- Колекція 1: List<Product> ----------
            Console.WriteLine("=== List<Product> ===");
            var products = new List<Product>
            {
                new Product { Name = "Ноутбук", Price = 32000, Category = "Техніка" },
                new Product { Name = "Миша", Price = 650, Category = "Аксесуари" },
                new Product { Name = "Клавіатура", Price = 1800, Category = "Аксесуари" },
                new Product { Name = "Монітор", Price = 8700, Category = "Техніка" },
                new Product { Name = "Килимок", Price = 300, Category = "Аксесуари" }
            };

            var names = CollectionUtils.Map(products, p => p.Name);
            Console.WriteLine("Map (назви): " + string.Join(", ", names));

            var mostExpensive = CollectionUtils.MaxBy(products, p => p.Price);
            Console.WriteLine($"MaxBy (найдорожчий): {mostExpensive.Name} - {mostExpensive.Price} грн");

            var longestName = CollectionUtils.MaxBy(products, p => p.Name.Length);
            Console.WriteLine($"MaxBy (найдовша назва): {longestName.Name}");

            var expensive = CollectionUtils.Filter(products, p => p.Price > 1000);
            Console.WriteLine("Filter (ціна > 1000):");
            CollectionUtils.ForEach(expensive, p => Console.WriteLine($"  - {p.Name}"));

            Console.WriteLine("ForEach (усі товари зі знижкою 10%):");
            CollectionUtils.ForEach(products, p => Console.WriteLine($"  {p.Name,-12} {p.Price,8} грн -> {p.Price * 0.9m,8:F0} грн"));

            // Ланцюжок: категорія "Аксесуари" -> рядки "Назва (ціна)"
            var accessories = CollectionUtils.Map(
                CollectionUtils.Filter(products, p => p.Category == "Аксесуари"),
                p => $"{p.Name} ({p.Price} грн)");
            Console.WriteLine("Filter + Map (аксесуари): " + string.Join("; ", accessories));
            Console.WriteLine();

            // ---------- Колекція 2: List<int> ----------
            Console.WriteLine("=== List<int> ===");
            var numbers = new List<int> { 7, -3, 12, 0, 25, -18, 4, 9 };
            Console.WriteLine("Вхідні дані: " + string.Join(", ", numbers));
            Console.WriteLine("Map (квадрати): " + string.Join(", ", CollectionUtils.Map(numbers, n => n * n)));
            Console.WriteLine("Map (int -> string): " + string.Join(", ", CollectionUtils.Map(numbers, n => n >= 0 ? $"+{n}" : n.ToString())));
            Console.WriteLine("Filter (парні): " + string.Join(", ", CollectionUtils.Filter(numbers, n => n % 2 == 0)));
            Console.WriteLine("Filter (від'ємні): " + string.Join(", ", CollectionUtils.Filter(numbers, n => n < 0)));
            Console.WriteLine($"MaxBy (найбільше за модулем): {CollectionUtils.MaxBy(numbers, n => Math.Abs(n))}");

            int sum = 0;
            CollectionUtils.ForEach(numbers, n => sum += n); // лямбда захоплює зовнішню змінну (замикання)
            Console.WriteLine($"ForEach (сума через замикання): {sum}");
            Console.WriteLine();

            // ---------- Колекція 3: string[] ----------
            Console.WriteLine("=== string[] ===");
            string[] words = { "делегат", "лямбда", "LINQ", "узагальнення", "C#", "предикат" };
            Console.WriteLine("Map (довжини): " + string.Join(", ", CollectionUtils.Map(words, w => $"{w}={w.Length}")));
            Console.WriteLine("Map (верхній регістр): " + string.Join(", ", CollectionUtils.Map(words, w => w.ToUpper())));
            Console.WriteLine("Filter (довжина > 6): " + string.Join(", ", CollectionUtils.Filter(words, w => w.Length > 6)));
            Console.WriteLine($"MaxBy (найдовше слово): {CollectionUtils.MaxBy(words, w => w.Length)}");
            Console.WriteLine($"MaxBy (останнє за алфавітом): {CollectionUtils.MaxBy(words, w => w)}");
            Console.Write("ForEach (перші літери): ");
            CollectionUtils.ForEach(words, w => Console.Write(w[0] + " "));
            Console.WriteLine();
            Console.WriteLine();

            // ---------- Колекція 4: Dictionary<string, int> ----------
            Console.WriteLine("=== Dictionary<string, int> (оцінки) ===");
            var grades = new Dictionary<string, int> { ["Іван"] = 78, ["Марія"] = 95, ["Олег"] = 61, ["Анна"] = 88 };
            var best = CollectionUtils.MaxBy(grades, g => g.Value);
            Console.WriteLine($"MaxBy: {best.Key} ({best.Value})");
            Console.WriteLine("Filter (>= 80): " + string.Join(", ", CollectionUtils.Map(CollectionUtils.Filter(grades, g => g.Value >= 80), g => g.Key)));
            Console.WriteLine();

            // ---------- Делегати як змінні ----------
            Console.WriteLine("=== Делегати як змінні (повторне використання логіки) ===");
            Predicate<int> isPositive = n => n > 0;
            Func<int, string> toStars = n => new string('*', Math.Min(n, 20));
            Action<string> print = s => Console.WriteLine($"  {s}");

            var positive = CollectionUtils.Filter(numbers, isPositive);
            CollectionUtils.ForEach(CollectionUtils.Map(positive, toStars), print);
            Console.WriteLine();

            // ---------- Порівняння з LINQ ----------
            Console.WriteLine("=== Порівняння з LINQ ===");
            var mine = CollectionUtils.Map(CollectionUtils.Filter(products, p => p.Price > 1000), p => p.Name);
            var linq = products.Where(p => p.Price > 1000).Select(p => p.Name);
            Console.WriteLine($"CollectionUtils: {string.Join(", ", mine)}");
            Console.WriteLine($"LINQ:            {string.Join(", ", linq)}");
            Console.WriteLine($"Результати однакові: {mine.SequenceEqual(linq)}");
            Console.WriteLine($"MaxBy однаковий: {CollectionUtils.MaxBy(products, p => p.Price).Name == products.MaxBy(p => p.Price).Name}");
        }
    }
}
