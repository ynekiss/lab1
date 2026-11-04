using System.Globalization;
using System.Text;

namespace lab8v20;

/// <summary>
/// Варіант 20. Статичний узагальнений клас-утиліта з методом Sum.
/// Поведінка (що саме підсумовувати) передається ззовні через делегат Func&lt;T, decimal&gt;.
/// </summary>
public static class CollectionHelper<T>
{
    /// <summary>
    /// Обчислює суму значень, які selector повертає для кожного елемента колекції.
    /// Для порожньої колекції повертає 0.
    /// </summary>
    public static decimal Sum(IEnumerable<T> collection, Func<T, decimal> selector)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(selector);

        decimal total = 0m;
        foreach (T item in collection)
        {
            total += selector(item); // виклик делегата для поточного елемента
        }
        return total;
    }
}

public class Product
{
    public string Name { get; }
    public string Category { get; }
    public decimal Price { get; }
    public int Quantity { get; }

    public Product(string name, string category, decimal price, int quantity)
    {
        Name = name;
        Category = category;
        Price = price;
        Quantity = quantity;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        CultureInfo.CurrentCulture = new CultureInfo("uk-UA");

        // ---------- Колекція 1: List<Product> ----------
        Console.WriteLine("=== List<Product> ===");
        List<Product> products = new List<Product>
        {
            new Product("Ноутбук", "Електроніка", 32000m, 2),
            new Product("Миша", "Аксесуари", 650m, 10),
            new Product("Монітор", "Електроніка", 8700m, 3),
            new Product("Клавіатура", "Аксесуари", 1800m, 5),
            new Product("Навушники", "Аксесуари", 2400m, 4)
        };

        decimal pricesSum = CollectionHelper<Product>.Sum(products, p => p.Price);
        Console.WriteLine($"Сума цін (по одному товару кожного виду): {pricesSum:N2} грн");

        decimal stockValue = CollectionHelper<Product>.Sum(products, p => p.Price * p.Quantity);
        Console.WriteLine($"Загальна вартість усіх товарів на складі: {stockValue:N2} грн");

        decimal electronicsValue = CollectionHelper<Product>.Sum(
            products,
            p => p.Category == "Електроніка" ? p.Price * p.Quantity : 0m);
        Console.WriteLine($"Вартість товарів категорії \"Електроніка\": {electronicsValue:N2} грн");

        // Лямбда, збережена в змінну-делегат, - логіку можна перевикористати
        Func<Product, decimal> discountedValue = p => p.Price * p.Quantity * 0.9m;
        Console.WriteLine($"Вартість складу зі знижкою 10%: {CollectionHelper<Product>.Sum(products, discountedValue):N2} грн");

        int itemsCount = (int)CollectionHelper<Product>.Sum(products, p => p.Quantity);
        Console.WriteLine($"Кількість одиниць товару на складі: {itemsCount}");
        Console.WriteLine();

        // ---------- Колекція 2: List<int> ----------
        Console.WriteLine("=== List<int> ===");
        List<int> numbers = new List<int> { 5, -3, 12, 8, -7, 10 };
        Console.WriteLine("Числа: " + string.Join(", ", numbers));
        Console.WriteLine($"Сума всіх чисел: {CollectionHelper<int>.Sum(numbers, n => n)}");
        Console.WriteLine($"Сума додатних чисел: {CollectionHelper<int>.Sum(numbers, n => n > 0 ? n : 0)}");
        Console.WriteLine($"Сума квадратів: {CollectionHelper<int>.Sum(numbers, n => n * n)}");
        Console.WriteLine();

        // ---------- Колекція 3: List<string> ----------
        Console.WriteLine("=== List<string> ===");
        List<string> words = new List<string> { "делегат", "лямбда", "узагальнення", "LINQ" };
        Console.WriteLine("Слова: " + string.Join(", ", words));
        Console.WriteLine($"Загальна кількість символів: {CollectionHelper<string>.Sum(words, w => w.Length)}");
        Console.WriteLine($"Кількість слів, довших за 5 символів: {CollectionHelper<string>.Sum(words, w => w.Length > 5 ? 1 : 0)}");
        Console.WriteLine();

        // ---------- Граничні випадки ----------
        Console.WriteLine("=== Граничні випадки ===");
        List<Product> emptyList = new List<Product>();
        Console.WriteLine($"Сума для порожньої колекції: {CollectionHelper<Product>.Sum(emptyList, p => p.Price)}");
        try
        {
            CollectionHelper<int>.Sum(null!, n => n);
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine($"Передано null замість колекції -> ArgumentNullException ({ex.ParamName})");
        }
        Console.WriteLine();

        // ---------- Порівняння з LINQ ----------
        Console.WriteLine("=== Порівняння з LINQ Sum ===");
        decimal linqValue = products.Sum(p => p.Price * p.Quantity);
        Console.WriteLine($"CollectionHelper: {stockValue:N2}, LINQ: {linqValue:N2}, збігаються: {stockValue == linqValue}");
    }
}
