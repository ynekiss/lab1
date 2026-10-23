using System.Globalization;
using System.Text;

namespace IndependentWork2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            // Українська культура, щоб формат :C виводив ціну в гривнях (₴)
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA");

            Console.WriteLine("Створення товарів");

            // Конструктор 1 - основний (усі 5 параметрів)
            Product laptop = new Product(101, "Laptop", 35000m, "Electronics", 15);

            // Конструктор 2 - скорочений (id, name, price)
            Product mouse = new Product(102, "Mouse", 800m);

            // Конструктор 3 - копіювання (дублікат об'єкта laptop)
            Product laptopCopy = new Product(laptop);

            Console.WriteLine($"Товар 1 (основний конструктор): {laptop}");
            Console.WriteLine($"Товар 2 (скорочений конструктор): {mouse}");
            Console.WriteLine($"Товар 3 (конструктор копіювання): {laptopCopy}");

            Console.WriteLine();
            Console.WriteLine("Перевірка копії:");
            Console.WriteLine($"laptop і laptopCopy - один і той самий об'єкт? {ReferenceEquals(laptop, laptopCopy)}");
            Console.WriteLine($"Дані збігаються? {laptop.ToString() == laptopCopy.ToString()}");
        }
    }
}
