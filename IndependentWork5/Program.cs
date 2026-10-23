using System.Globalization;
using System.Text;

namespace IndependentWork5
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA"); // українське форматування чисел і дат

            ObjectPolymorphism();
            EnumerablePolymorphism();
            StreamPolymorphism();
        }

        // ---------- Сценарій 1: System.Object.ToString() ----------
        private static void ObjectPolymorphism()
        {
            Console.WriteLine("=== Сценарій 1. Поліморфізм через System.Object ===");

            Customer customer = new Customer("Андрій Мельник", "andrii@example.com");
            Product keyboard = new Product("Клавіатура", 1500m);
            Product mouse = new Product("Миша", 650m);
            Order order = new Order(42, customer, new List<Product> { keyboard, mouse });

            List<object> objects = new List<object>
            {
                customer, keyboard, order,
                new Warehouse(),   // ToString() не перевизначено
                2026,              // int теж є object
                "просто рядок"
            };

            foreach (object obj in objects)
            {
                // Змінна має тип object, але викликається ToString() фактичного типу
                Console.WriteLine($"{obj.GetType().Name,-10} -> {obj}");
            }
            Console.WriteLine();
        }

        // ---------- Сценарій 2: IEnumerable<T> ----------
        private static void EnumerablePolymorphism()
        {
            Console.WriteLine("=== Сценарій 2. Поліморфізм через IEnumerable<string> ===");

            MyCustomCollection languages = new MyCustomCollection();
            languages.Add("C#");
            languages.Add("Java");
            languages.Add("Python");
            languages.Add("Go");

            Console.WriteLine("foreach по MyCustomCollection:");
            foreach (string lang in languages)
            {
                Console.WriteLine($"  - {lang}");
            }

            // Один і той самий метод працює з будь-якою реалізацією IEnumerable<string>
            string[] array = { "яблуко", "груша" };
            List<string> list = new List<string> { "один", "два", "три" };

            PrintAll("MyCustomCollection", languages);
            PrintAll("string[]", array);
            PrintAll("List<string>", list);

            // LINQ теж працює, бо він написаний для IEnumerable<T>
            Console.WriteLine("LINQ по MyCustomCollection (довжина > 2): " +
                string.Join(", ", languages.Where(l => l.Length > 2)));
            Console.WriteLine();
        }

        private static void PrintAll(string title, IEnumerable<string> items)
        {
            Console.WriteLine($"PrintAll({title}): {string.Join(" | ", items)}");
        }

        // ---------- Сценарій 3: абстрактний клас Stream ----------
        private static void StreamPolymorphism()
        {
            Console.WriteLine("=== Сценарій 3. Поліморфізм через абстрактний клас Stream ===");

            // Тестовий файл створюється в каталозі проєкту, тому розв'язок самодостатній
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "test-data.txt");
            // UTF8Encoding(false) - без BOM, щоб перші байти файлу були саме текстом
            File.WriteAllText(filePath, "Hello from FileStream! Привіт з файлу.", new UTF8Encoding(false));

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                ProcessStream(fileStream);
            }

            byte[] bytes = Encoding.UTF8.GetBytes("Hello from MemoryStream! Дані в пам'яті.");
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                ProcessStream(memoryStream);
            }
        }

        /// <summary>
        /// Метод знає лише про абстрактний Stream і не знає, звідки беруться байти.
        /// </summary>
        private static void ProcessStream(Stream stream)
        {
            Console.WriteLine($"ProcessStream отримав: {stream.GetType().Name}");
            Console.WriteLine($"  Length = {stream.Length} байт, CanRead = {stream.CanRead}, CanSeek = {stream.CanSeek}");

            byte[] buffer = new byte[16];
            int read = stream.Read(buffer, 0, buffer.Length); // викликається Read() конкретного класу
            Console.WriteLine($"  Прочитано {read} байт: \"{Encoding.UTF8.GetString(buffer, 0, read)}\"");
            Console.WriteLine($"  Перші байти (hex): {BitConverter.ToString(buffer, 0, Math.Min(read, 8))}");
        }
    }
}
