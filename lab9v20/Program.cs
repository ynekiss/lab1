using System.Text;

namespace lab9v20;

/// <summary>
/// Користувач, для якого підбираються рекомендації.
/// </summary>
public class User
{
    public string Name { get; }
    public HashSet<string> Purchased { get; }

    public User(string name, params string[] purchased)
    {
        Name = name;
        Purchased = new HashSet<string>(purchased, StringComparer.OrdinalIgnoreCase);
    }

    public override string ToString() => Name;
}

/// <summary>
/// Варіант 20. Система рекомендацій:
/// HashSet - унікальні продукти для рекомендацій,
/// Queue   - черга користувачів для обробки,
/// Stack   - історія переглянутих рекомендацій.
/// </summary>
public class RecommendationSystem
{
    private readonly HashSet<string> _products = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<User> _userQueue = new Queue<User>();
    private readonly Stack<string> _viewHistory = new Stack<string>();

    // ---------- HashSet: унікальні продукти ----------

    /// <summary>Додає продукт. Повертає false, якщо такий уже є (дублікат).</summary>
    public bool AddProduct(string product) => _products.Add(product);

    /// <summary>Додає продукти з іншого джерела (UnionWith). Повертає кількість нових.</summary>
    public int AddProducts(IEnumerable<string> products)
    {
        int before = _products.Count;
        _products.UnionWith(products);
        return _products.Count - before;
    }

    public bool RemoveProduct(string product) => _products.Remove(product);

    public bool HasProduct(string product) => _products.Contains(product);

    public int ProductCount => _products.Count;

    public IEnumerable<string> Products => _products.OrderBy(p => p, StringComparer.Ordinal);

    // ---------- Queue: черга користувачів ----------

    public void EnqueueUser(User user) => _userQueue.Enqueue(user);

    public int UsersInQueue => _userQueue.Count;

    /// <summary>Хто наступний у черзі (без видалення).</summary>
    public User PeekNextUser() => _userQueue.Peek();

    /// <summary>
    /// Забирає першого користувача з черги (FIFO) і підбирає йому до 3 продуктів,
    /// яких він ще не купував (ExceptWith).
    /// </summary>
    public (User User, List<string> Recommendations) ProcessNextUser()
    {
        User user = _userQueue.Dequeue();

        HashSet<string> candidates = new HashSet<string>(_products, StringComparer.OrdinalIgnoreCase);
        candidates.ExceptWith(user.Purchased);

        List<string> recommendations = candidates
            .OrderBy(p => p, StringComparer.Ordinal)
            .Take(3)
            .ToList();

        return (user, recommendations);
    }

    // ---------- Stack: історія переглянутих рекомендацій ----------

    public void ViewRecommendation(string product) => _viewHistory.Push(product);

    public int HistoryCount => _viewHistory.Count;

    /// <summary>Що зараз відкрито (вершина стека) або null, якщо історія порожня.</summary>
    public string? CurrentView => _viewHistory.Count > 0 ? _viewHistory.Peek() : null;

    /// <summary>Кнопка "Назад": закриває останню переглянуту рекомендацію (Pop).</summary>
    public string? GoBack() => _viewHistory.Count > 0 ? _viewHistory.Pop() : null;

    /// <summary>Історія від останнього перегляду до першого.</summary>
    public IEnumerable<string> History => _viewHistory;
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        RecommendationSystem system = new RecommendationSystem();

        HashSetDemo(system);
        QueueDemo(system);
        StackDemo(system);
        OrderComparison();
    }

    private static void HashSetDemo(RecommendationSystem system)
    {
        Console.WriteLine("=== 1. HashSet<string>: унікальні продукти для рекомендацій ===");

        string[] popular = { "Ноутбук", "Смартфон", "Навушники", "Мишка" };
        foreach (string product in popular)
        {
            Console.WriteLine($"Add(\"{product}\") = {system.AddProduct(product)}");
        }

        Console.WriteLine($"Add(\"Смартфон\") = {system.AddProduct("Смартфон")}  <- дублікат не додається");
        Console.WriteLine($"Add(\"навушники\") = {system.AddProduct("навушники")}  <- регістр не враховується");

        string[] newArrivals = { "Монітор", "Мишка", "Планшет", "Клавіатура" };
        int added = system.AddProducts(newArrivals);
        Console.WriteLine($"UnionWith(новинки: {string.Join(", ", newArrivals)}) -> додано нових: {added}");
        Console.WriteLine($"Кількість унікальних продуктів: {system.ProductCount}");

        Console.WriteLine($"Contains(\"Планшет\") = {system.HasProduct("Планшет")}");
        Console.WriteLine($"Contains(\"Телевізор\") = {system.HasProduct("Телевізор")}");

        Console.WriteLine($"Remove(\"Мишка\") = {system.RemoveProduct("Мишка")}  <- знято з продажу");
        Console.WriteLine($"Продукти для рекомендацій ({system.ProductCount}): {string.Join(", ", system.Products)}");

        // Типовий сценарій HashSet - прибрати дублікати зі списку переглядів
        List<string> views = new List<string> { "Смартфон", "Ноутбук", "Смартфон", "Навушники", "Ноутбук", "Смартфон" };
        HashSet<string> uniqueViews = new HashSet<string>(views);
        Console.WriteLine($"Перегляди (List, {views.Count} записів): {string.Join(", ", views)}");
        Console.WriteLine($"Унікальні перегляди (HashSet, {uniqueViews.Count}): {string.Join(", ", uniqueViews.OrderBy(v => v, StringComparer.Ordinal))}");
        Console.WriteLine();
    }

    private static void QueueDemo(RecommendationSystem system)
    {
        Console.WriteLine("=== 2. Queue<User>: черга користувачів для обробки ===");

        User[] users =
        {
            new User("Олена", "Смартфон", "Навушники"),
            new User("Андрій", "Ноутбук"),
            new User("Ірина", "Клавіатура", "Монітор", "Планшет")
        };

        foreach (User user in users)
        {
            system.EnqueueUser(user);
            Console.WriteLine($"Enqueue: {user} (купив(ла): {string.Join(", ", user.Purchased)}) -> у черзі: {system.UsersInQueue}");
        }

        Console.WriteLine($"Peek: наступний(а) - {system.PeekNextUser()} (з черги не видаляється, у черзі: {system.UsersInQueue})");

        while (system.UsersInQueue > 0)
        {
            var (user, recommendations) = system.ProcessNextUser();
            Console.WriteLine($"Dequeue: {user} -> рекомендації: {string.Join(", ", recommendations)} (залишилось у черзі: {system.UsersInQueue})");
        }

        try
        {
            system.ProcessNextUser();
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Dequeue на порожній черзі -> InvalidOperationException");
        }
        Console.WriteLine();
    }

    private static void StackDemo(RecommendationSystem system)
    {
        Console.WriteLine("=== 3. Stack<string>: історія переглянутих рекомендацій ===");

        string[] opened = { "Клавіатура", "Монітор", "Ноутбук" };
        foreach (string product in opened)
        {
            system.ViewRecommendation(product);
            Console.WriteLine($"Push: відкрито \"{product}\" (в історії: {system.HistoryCount})");
        }

        Console.WriteLine($"Peek: зараз відкрито - {system.CurrentView}");
        Console.WriteLine($"Історія (від останнього): {string.Join(" <- ", system.History)}");

        string? closed = system.GoBack();
        Console.WriteLine($"Назад (Pop): закрито \"{closed}\", тепер відкрито - {system.CurrentView}");

        system.ViewRecommendation("Планшет");
        Console.WriteLine("Push: відкрито \"Планшет\"");
        Console.WriteLine($"Історія (від останнього): {string.Join(" <- ", system.History)}");

        string? product1;
        while ((product1 = system.GoBack()) != null)
        {
            Console.WriteLine($"Pop: {product1} (в історії: {system.HistoryCount})");
        }

        Console.WriteLine(system.GoBack() == null
            ? "Історія порожня - повертатися нікуди"
            : "Помилка: історія не порожня");
        Console.WriteLine();
    }

    /// <summary>Однакові елементи в Queue і Stack виходять у протилежному порядку.</summary>
    private static void OrderComparison()
    {
        Console.WriteLine("=== 4. Порівняння порядку обробки: FIFO vs LIFO ===");
        string[] items = { "Ноутбук", "Смартфон", "Планшет" };

        Queue<string> queue = new Queue<string>(items);
        Stack<string> stack = new Stack<string>(items);

        Console.WriteLine($"Додано по черзі:     {string.Join(" -> ", items)}");

        List<string> fromQueue = new List<string>();
        while (queue.Count > 0)
        {
            fromQueue.Add(queue.Dequeue());
        }

        List<string> fromStack = new List<string>();
        while (stack.Count > 0)
        {
            fromStack.Add(stack.Pop());
        }

        Console.WriteLine($"Queue (FIFO) віддає: {string.Join(" -> ", fromQueue)}");
        Console.WriteLine($"Stack (LIFO) віддає: {string.Join(" -> ", fromStack)}");
    }
}
