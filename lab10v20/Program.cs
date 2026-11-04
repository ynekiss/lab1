using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace lab10v20;

// ===================== Сутності (варіант 20) =====================

public class Order
{
    public int Id { get; }
    public DateTime OrderDate { get; }
    public int CustomerId { get; }

    public Order(int id, DateTime orderDate, int customerId)
    {
        Id = id;
        OrderDate = orderDate;
        CustomerId = customerId;
    }
}

public class OrderDetail
{
    public int Id { get; }
    public int OrderId { get; }
    public int ProductId { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }

    public OrderDetail(int id, int orderId, int productId, int quantity, decimal unitPrice)
    {
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

// ===================== Тестові дані =====================

public static class TestData
{
    /// <summary>
    /// Дата в поточному місяці. День обмежується сьогоднішнім числом,
    /// щоб замовлення не опинялось у майбутньому.
    /// </summary>
    public static DateTime ThisMonth(int day)
    {
        DateTime today = DateTime.Today;
        return new DateTime(today.Year, today.Month, Math.Min(day, today.Day));
    }

    public static DateTime MonthsAgo(int months, int day)
    {
        DateTime start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-months);
        return start.AddDays(day - 1);
    }

    /// <summary>12 замовлень: 6 з минулих місяців і 6 з поточного.</summary>
    public static List<Order> CreateOrders() => new List<Order>
    {
        new Order(1, MonthsAgo(2, 5), 1),
        new Order(2, MonthsAgo(2, 18), 2),
        new Order(3, MonthsAgo(1, 3), 3),
        new Order(4, MonthsAgo(1, 11), 1),
        new Order(5, MonthsAgo(1, 20), 4),
        new Order(6, MonthsAgo(1, 27), 2),
        new Order(7, ThisMonth(1), 5),
        new Order(8, ThisMonth(1), 3),
        new Order(9, ThisMonth(2), 1),
        new Order(10, ThisMonth(3), 4),
        new Order(11, ThisMonth(3), 2),
        new Order(12, ThisMonth(4), 5)
    };

    /// <summary>20 рядків деталей замовлень, товари з ProductId 101-106.</summary>
    public static List<OrderDetail> CreateDetails() => new List<OrderDetail>
    {
        new OrderDetail(1, 1, 101, 2, 100m),
        new OrderDetail(2, 1, 102, 1, 250m),
        new OrderDetail(3, 2, 103, 5, 40m),
        new OrderDetail(4, 3, 101, 1, 100m),
        new OrderDetail(5, 3, 104, 3, 75m),
        new OrderDetail(6, 4, 105, 10, 12.5m),
        new OrderDetail(7, 5, 102, 2, 250m),
        new OrderDetail(8, 6, 101, 4, 100m),
        new OrderDetail(9, 6, 106, 1, 999m),
        new OrderDetail(10, 7, 101, 3, 100m),
        new OrderDetail(11, 7, 103, 2, 40m),
        new OrderDetail(12, 8, 104, 6, 75m),
        new OrderDetail(13, 8, 105, 8, 12.5m),
        new OrderDetail(14, 9, 101, 1, 100m),
        new OrderDetail(15, 9, 102, 2, 250m),
        new OrderDetail(16, 10, 106, 2, 999m),
        new OrderDetail(17, 11, 103, 7, 40m),
        new OrderDetail(18, 11, 101, 5, 100m),
        new OrderDetail(19, 12, 105, 4, 12.5m),
        new OrderDetail(20, 12, 104, 1, 75m)
    };

    /// <summary>Великий набір для аналізу продуктивності (Random з фіксованим seed).</summary>
    public static (List<Order> Orders, List<OrderDetail> Details) CreateLarge(int orderCount, int detailCount)
    {
        Random random = new Random(42);
        List<Order> orders = new List<Order>(orderCount);
        for (int i = 1; i <= orderCount; i++)
        {
            DateTime date = i % 3 == 0 ? ThisMonth(random.Next(1, 29)) : MonthsAgo(random.Next(1, 6), random.Next(1, 29));
            orders.Add(new Order(i, date, random.Next(1, 200)));
        }

        List<OrderDetail> details = new List<OrderDetail>(detailCount);
        for (int i = 1; i <= detailCount; i++)
        {
            details.Add(new OrderDetail(i, random.Next(1, orderCount + 1), random.Next(101, 151), random.Next(1, 11), random.Next(10, 1000)));
        }
        return (orders, details);
    }
}

// ===================== Вимірювання часу =====================

public class Measurement
{
    public string Query { get; init; } = "";
    public string Syntax { get; init; } = "";
    public double FirstRunMs { get; init; }
    public double AverageMicroseconds { get; init; }
}

public static class QueryTimer
{
    public static List<Measurement> Results { get; } = new List<Measurement>();

    /// <summary>
    /// Вимірює запит через Stopwatch: окремо перший запуск (з JIT-компіляцією)
    /// і середній час з repeats повторів. ToList() у запиті змушує його виконатись.
    /// </summary>
    public static T Measure<T>(string query, string syntax, Func<T> run, int repeats)
    {
        Stopwatch sw = Stopwatch.StartNew();
        T result = run();
        sw.Stop();
        double firstMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        for (int i = 0; i < repeats; i++)
        {
            result = run();
        }
        sw.Stop();
        double avgMicro = sw.Elapsed.TotalMilliseconds * 1000 / repeats;

        Results.Add(new Measurement { Query = query, Syntax = syntax, FirstRunMs = firstMs, AverageMicroseconds = avgMicro });
        Console.WriteLine($"  [{syntax}] перший запуск: {firstMs:F3} мс, середнє з {repeats}: {avgMicro:F2} мкс");
        return result;
    }
}

// ===================== Програма =====================

public class Program
{
    private const int TargetProductId = 101;
    private const int Repeats = 1000;

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        CultureInfo.CurrentCulture = new CultureInfo("uk-UA");

        List<Order> orders = TestData.CreateOrders();
        List<OrderDetail> details = TestData.CreateDetails();
        Console.WriteLine($"Тестові дані: {orders.Count} замовлень, {details.Count} рядків деталей замовлень");
        Console.WriteLine();

        Query1(orders, details);
        Query2(orders, details);
        Query3(details);
        Query4(orders, details);
        DeferredExecutionDemo(details);
        LargeDataBenchmark();
        PrintSummary();
    }

    private static bool IsCurrentMonth(DateTime date) =>
        date.Year == DateTime.Today.Year && date.Month == DateTime.Today.Month;

    // ---------- Запит 1: фільтрація ----------
    private static void Query1(List<Order> orders, List<OrderDetail> details)
    {
        Console.WriteLine($"=== Запит 1. Замовлення поточного місяця, що містять ProductId = {TargetProductId} ===");

        List<Order> querySyntax = QueryTimer.Measure("Запит 1", "запити", () =>
            (from o in orders
             where IsCurrentMonth(o.OrderDate)
                   && (from d in details
                       where d.OrderId == o.Id && d.ProductId == TargetProductId
                       select d).Any()
             select o).ToList(), Repeats);

        List<Order> methodSyntax = QueryTimer.Measure("Запит 1", "методи", () =>
            orders.Where(o => IsCurrentMonth(o.OrderDate)
                              && details.Any(d => d.OrderId == o.Id && d.ProductId == TargetProductId))
                  .ToList(), Repeats);

        foreach (Order o in methodSyntax)
        {
            Console.WriteLine($"  Замовлення #{o.Id}, клієнт {o.CustomerId}");
        }
        Console.WriteLine($"  Знайдено: {methodSyntax.Count}, результати обох синтаксисів однакові: {querySyntax.SequenceEqual(methodSyntax)}");
        Console.WriteLine();
    }

    // ---------- Запит 2: об'єднання (join) ----------
    private static void Query2(List<Order> orders, List<OrderDetail> details)
    {
        Console.WriteLine("=== Запит 2. Join замовлень з деталями: OrderId, ProductId, Quantity ===");

        var querySyntax = QueryTimer.Measure("Запит 2", "запити", () =>
            (from o in orders
             join d in details on o.Id equals d.OrderId
             select new { OrderId = o.Id, d.ProductId, d.Quantity }).ToList(), Repeats);

        var methodSyntax = QueryTimer.Measure("Запит 2", "методи", () =>
            orders.Join(details,
                        o => o.Id,
                        d => d.OrderId,
                        (o, d) => new { OrderId = o.Id, d.ProductId, d.Quantity })
                  .ToList(), Repeats);

        Console.WriteLine("  OrderId | ProductId | Quantity");
        foreach (var row in methodSyntax)
        {
            Console.WriteLine($"  {row.OrderId,7} | {row.ProductId,9} | {row.Quantity,8}");
        }
        Console.WriteLine($"  Рядків: {methodSyntax.Count}, результати обох синтаксисів однакові: {querySyntax.SequenceEqual(methodSyntax)}");
        Console.WriteLine();
    }

    // ---------- Запит 3: групування та агрегація ----------
    private static void Query3(List<OrderDetail> details)
    {
        Console.WriteLine("=== Запит 3. Загальна кількість замовлених одиниць для кожного продукту ===");

        var querySyntax = QueryTimer.Measure("Запит 3", "запити", () =>
            (from d in details
             group d by d.ProductId into g
             orderby g.Key
             select new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) }).ToList(), Repeats);

        var methodSyntax = QueryTimer.Measure("Запит 3", "методи", () =>
            details.GroupBy(d => d.ProductId)
                   .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                   .OrderBy(x => x.ProductId)
                   .ToList(), Repeats);

        foreach (var row in methodSyntax)
        {
            Console.WriteLine($"  ProductId {row.ProductId}: {row.TotalQuantity} од.");
        }
        Console.WriteLine($"  Усього одиниць: {methodSyntax.Sum(x => x.TotalQuantity)}, результати обох синтаксисів однакові: {querySyntax.SequenceEqual(methodSyntax)}");
        Console.WriteLine();
    }

    // ---------- Запит 4: агрегація + пошук максимуму ----------
    private static void Query4(List<Order> orders, List<OrderDetail> details)
    {
        Console.WriteLine("=== Запит 4. Замовлення з найбільшою загальною кількістю одиниць ===");

        var querySyntax = QueryTimer.Measure("Запит 4", "запити", () =>
            (from d in details
             group d by d.OrderId into g
             let total = g.Sum(x => x.Quantity)
             orderby total descending, g.Key
             select new { OrderId = g.Key, TotalQuantity = total }).First(), Repeats);

        var methodSyntax = QueryTimer.Measure("Запит 4", "методи", () =>
            details.GroupBy(d => d.OrderId)
                   .Select(g => new { OrderId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                   .MaxBy(x => x.TotalQuantity)!, Repeats);

        Order order = orders.First(o => o.Id == methodSyntax.OrderId);
        decimal amount = details.Where(d => d.OrderId == order.Id).Sum(d => d.Quantity * d.UnitPrice);
        Console.WriteLine($"  Замовлення #{order.Id} (клієнт {order.CustomerId}): {methodSyntax.TotalQuantity} од. на суму {amount:N2} грн");
        Console.WriteLine($"  Результати обох синтаксисів однакові: {querySyntax.Equals(methodSyntax)}");
        Console.WriteLine();
    }

    // ---------- Відкладене виконання ----------
    private static void DeferredExecutionDemo(List<OrderDetail> source)
    {
        Console.WriteLine("=== Відкладене та негайне виконання ===");
        List<OrderDetail> details = new List<OrderDetail>(source); // копія, щоб не змінити основні дані

        IEnumerable<OrderDetail> deferred = details.Where(d => d.Quantity >= 8); // запит лише створено
        List<OrderDetail> immediate = details.Where(d => d.Quantity >= 8).ToList(); // виконано одразу

        details.Add(new OrderDetail(21, 12, 101, 9, 100m)); // новий рядок ПІСЛЯ створення запитів

        Console.WriteLine($"  Відкладений запит (Where без ToList): {deferred.Count()} рядки(ів) - бачить новий рядок");
        Console.WriteLine($"  Негайний запит (Where + ToList): {immediate.Count} рядки(ів) - результат зафіксовано раніше");
        Console.WriteLine();
    }

    // ---------- Продуктивність на великих даних ----------
    private static void LargeDataBenchmark()
    {
        const int largeRepeats = 10;
        var (orders, details) = TestData.CreateLarge(2000, 10000);
        Console.WriteLine($"=== Продуктивність на великих даних: {orders.Count} замовлень, {details.Count} деталей ===");

        Console.WriteLine("Запит 1 (Where + вкладений Any):");
        QueryTimer.Measure("Великі дані: запит 1", "методи", () =>
            orders.Where(o => IsCurrentMonth(o.OrderDate)
                              && details.Any(d => d.OrderId == o.Id && d.ProductId == TargetProductId))
                  .ToList(), largeRepeats);

        Console.WriteLine("Запит 1 (оптимізовано: спершу HashSet потрібних OrderId):");
        QueryTimer.Measure("Великі дані: запит 1 через HashSet", "методи", () =>
        {
            HashSet<int> orderIds = details.Where(d => d.ProductId == TargetProductId)
                                           .Select(d => d.OrderId)
                                           .ToHashSet();
            return orders.Where(o => IsCurrentMonth(o.OrderDate) && orderIds.Contains(o.Id)).ToList();
        }, largeRepeats);

        Console.WriteLine("Запит 2 (Join):");
        QueryTimer.Measure("Великі дані: запит 2", "методи", () =>
            orders.Join(details, o => o.Id, d => d.OrderId,
                        (o, d) => new { OrderId = o.Id, d.ProductId, d.Quantity }).ToList(), largeRepeats);

        Console.WriteLine("Запит 3 (GroupBy + Sum):");
        QueryTimer.Measure("Великі дані: запит 3", "методи", () =>
            details.GroupBy(d => d.ProductId)
                   .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                   .OrderBy(x => x.ProductId)
                   .ToList(), largeRepeats);

        Console.WriteLine("Запит 4 (GroupBy + Sum + MaxBy):");
        QueryTimer.Measure("Великі дані: запит 4", "методи", () =>
            details.GroupBy(d => d.OrderId)
                   .Select(g => new { OrderId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                   .MaxBy(x => x.TotalQuantity)!, largeRepeats);
        Console.WriteLine();
    }

    private static void PrintSummary()
    {
        Console.WriteLine("=== Підсумкова таблиця часу виконання ===");
        Console.WriteLine($"{"Запит",-36} | {"Синтаксис",-9} | {"Перший запуск, мс",17} | {"Середнє, мкс",13}");
        Console.WriteLine(new string('-', 84));
        foreach (Measurement m in QueryTimer.Results)
        {
            Console.WriteLine($"{m.Query,-36} | {m.Syntax,-9} | {m.FirstRunMs,17:F3} | {m.AverageMicroseconds,13:F2}");
        }
    }
}
