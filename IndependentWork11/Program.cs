// Самостійна робота №17. LINQ pipelines: ETL та аналітика.
// Проєкт IndependentWork11. Детальний звіт — у файлі REPORT.md.

using System.Diagnostics;
using System.Text;
using IndependentWork11;

Console.OutputEncoding = Encoding.UTF8;

const int Seed = 2026;
const int SalesCount = 1000;

// ---------- EXTRACT ----------
var rnd = new Random(Seed);
var products = DataGenerator.GenerateProducts(rnd);
var customers = DataGenerator.GenerateCustomers(rnd, 100);
var sales = DataGenerator.GenerateSales(rnd, products, customers, SalesCount);

Console.WriteLine("=== Самостійна робота №17: LINQ pipelines (ETL та аналітика) ===");
Console.WriteLine($"Дані (Seed = {Seed}): продуктів = {products.Count}, клієнтів = {customers.Count}, продажів = {sales.Count}");
Console.WriteLine();

// ---------- TRANSFORM + LOAD ----------
var analytics = new SalesAnalytics(sales, products, customers);

Run("Пайплайн 1: Найпопулярніші продукти за категоріями (топ-3)", () => analytics.TopProductsByCategory(), result =>
{
    foreach (var cat in result)
    {
        Console.WriteLine($"  {cat.Category}:");
        int place = 1;
        foreach (var p in cat.TopProducts)
            Console.WriteLine($"     {place++}. {p.ProductName,-18} {p.TotalQuantity,5} шт.");
    }
});

Run("Пайплайн 2: Загальний дохід за містами клієнтів", analytics.RevenueByCity, result =>
{
    Console.WriteLine($"  {"Місто",-14} {"Продажів",9} {"Дохід, грн",18}");
    foreach (var r in result)
        Console.WriteLine($"  {r.City,-14} {r.SalesCount,9} {r.Revenue,18:N2}");
    Console.WriteLine($"  {"РАЗОМ",-14} {result.Sum(r => r.SalesCount),9} {result.Sum(r => r.Revenue),18:N2}");
});

Run("Пайплайн 3: Продажі за місяцями та роками", analytics.SalesByMonth, result =>
{
    Console.WriteLine($"  {"Рік",-6} {"Місяць",-7} {"Одиниць",8} {"Дохід, грн",18}");
    foreach (var m in result)
        Console.WriteLine($"  {m.Year,-6} {m.Month,-7:D2} {m.TotalQuantity,8} {m.TotalRevenue,18:N2}");
});

Run("Пайплайн 4 (додатковий): Топ-5 клієнтів за витратами", () => analytics.TopCustomers(), result =>
{
    Console.WriteLine($"  {"Клієнт",-22} {"Місто",-13} {"Покупок",7} {"Витрачено, грн",16} {"Сер. чек",12}  Улюблена категорія");
    foreach (var c in result)
        Console.WriteLine($"  {c.CustomerName,-22} {c.City,-13} {c.Purchases,7} {c.TotalSpent,16:N2} {c.AverageCheck,12:N2}  {c.FavoriteCategory}");
});

// ---------- ЕКСПЕРИМЕНТ З ПРОДУКТИВНІСТЮ ----------
Console.WriteLine("=== Масштабування: середній час (мс) 5 «теплих» запусків для різних обсягів продажів ===");
Console.WriteLine($"{"Продажів",10} | {"П1 Топ/кат.",12} | {"П2 Join",10} | {"П2 Dict",10} | {"П3 Місяці",10} | {"П4 Клієнти",11}");
Console.WriteLine(new string('-', 78));

foreach (int n in new[] { 1_000, 10_000, 100_000, 1_000_000 })
{
    var r = new Random(Seed);
    var p = DataGenerator.GenerateProducts(r);
    var c = DataGenerator.GenerateCustomers(r, 100);
    var s = DataGenerator.GenerateSales(r, p, c, n);
    var a = new SalesAnalytics(s, p, c);

    Console.WriteLine($"{n,10:N0} | {Avg(() => a.TopProductsByCategory()),12:F3} | {Avg(a.RevenueByCity),10:F3} | " +
                      $"{Avg(a.RevenueByCityWithDictionaries),10:F3} | {Avg(a.SalesByMonth),10:F3} | {Avg(() => a.TopCustomers()),11:F3}");
}

// Перевірка коректності альтернативної реалізації пайплайна 2
var check1 = analytics.RevenueByCity();
var check2 = analytics.RevenueByCityWithDictionaries();
bool same = check1.Zip(check2).All(t => t.First.City == t.Second.City && t.First.Revenue == t.Second.Revenue);
Console.WriteLine();
Console.WriteLine($"Перевірка: Join-версія і Dictionary-версія пайплайна 2 дають однаковий результат: {same}");

// ---------- допоміжні методи ----------

// Окремий Stopwatch для кожного пайплайну: перший ("холодний") запуск включає JIT-компіляцію.
static void Run<T>(string title, Func<List<T>> pipeline, Action<List<T>> print)
{
    Console.WriteLine($"--- {title} ---");

    var cold = Stopwatch.StartNew();
    var result = pipeline();
    cold.Stop();

    double warm = Avg(pipeline);

    print(result);
    Console.WriteLine($"  Час: перший запуск {cold.Elapsed.TotalMilliseconds:F3} мс; " +
                      $"середній повторний {warm:F3} мс (5 запусків)");
    Console.WriteLine();
}

static double Avg<T>(Func<List<T>> pipeline, int runs = 5)
{
    pipeline(); // прогрів
    double total = 0;
    for (int i = 0; i < runs; i++)
    {
        var sw = Stopwatch.StartNew();
        var list = pipeline();
        sw.Stop();
        GC.KeepAlive(list);
        total += sw.Elapsed.TotalMilliseconds;
    }
    return total / runs;
}
