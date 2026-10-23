using System.Text;

namespace IndependentWork4
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("=== Крок 3. Початкова ієрархія: Book і Journal ===");
            Book book = new Book("BK-001", "Тіні забутих предків", 1911, "Михайло Коцюбинський");
            Journal journal = new Journal("JR-014", "Наука і життя", 2025, 7);
            book.DisplayDetails();
            journal.DisplayDetails();

            Console.WriteLine();
            Console.WriteLine("=== Крок 5. Розширена ієрархія: AudioBook : Book ===");
            AudioBook audioBook = new AudioBook("AB-203", "Кайдашева сім'я", 1879, "Іван Нечуй-Левицький", "Остап Ступка");
            audioBook.DisplayDetails();

            Console.WriteLine();
            Console.WriteLine("=== Поліморфний виклик через List<LibraryItem> ===");
            List<LibraryItem> catalog = new List<LibraryItem> { book, journal, audioBook };
            foreach (LibraryItem item in catalog)
            {
                // Тип змінної - LibraryItem, але викликається override фактичного класу
                Console.WriteLine($"{item.GetType().Name}:");
                item.DisplayDetails();
            }

            Console.WriteLine();
            Console.WriteLine($"audioBook is Book: {audioBook is Book}");
            Console.WriteLine($"audioBook is LibraryItem: {audioBook is LibraryItem}");
            Console.WriteLine($"Унікальний ID доступний у всіх нащадків: {audioBook.UniqueId}");
        }
    }
}
