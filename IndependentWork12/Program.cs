// Самостійна робота №18. PLINQ: дослідження продуктивності та безпеки.
// Проєкт IndependentWork12. Детальний звіт — у файлі REPORT.md.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

const int Seed = 42;
const int MaxValue = 10_000_000;   // діапазон випадкових чисел [2; 10 000 000)
const int Runs = 3;                // кількість вимірювань для усереднення

Console.WriteLine("=== Самостійна робота №18: PLINQ — продуктивність і безпека ===");
Console.WriteLine($"Логічних процесорів: {Environment.ProcessorCount}; .NET {Environment.Version}; " +
                  $"{(Debugger.IsAttached ? "Debugger" : "без відладчика")}");
Console.WriteLine();

// =====================================================================
// ЧАСТИНА 1. Порівняння продуктивності LINQ і PLINQ
// =====================================================================
int[] sizes = { 1_000_000, 5_000_000, 10_000_000 };

Console.WriteLine("--- Частина 1. Продуктивність (середнє з " + Runs + " запусків після прогріву) ---");
Console.WriteLine();
Console.WriteLine("Операція А (дорога): Where(IsPrime).Select(ScoreOf) → Sum");
Console.WriteLine("  IsPrime — перевірка на просте число перебором дільників до √n; " +
                  "ScoreOf — Math.Sqrt, Math.Log, Math.Sin.");
Console.WriteLine("Операція Б (дешева): Where(x % 2 == 0).Select(x * 2L) → Sum");
Console.WriteLine();
Console.WriteLine($"{"Розмір",12} | {"Операція",9} | {"LINQ, мс",10} | {"PLINQ, мс",10} | {"Прискорення",11} | Результати збігаються");
Console.WriteLine(new string('-', 88));

foreach (int size in sizes)
{
    List<int> data = GenerateData(size, Seed);

    // --- Операція А: обчислювально інтенсивна ---
    var (linqA, linqResA) = Measure(() => data.Where(IsPrime).Select(ScoreOf).Sum());
    var (plinqA, plinqResA) = Measure(() => data.AsParallel().Where(IsPrime).Select(ScoreOf).Sum());
    bool sameA = Math.Abs(linqResA - plinqResA) < 1e-6 * Math.Abs(linqResA); // double: порядок додавання різний
    PrintRow(size, "А дорога", linqA, plinqA, sameA);

    // --- Операція Б: дешева ---
    var (linqB, linqResB) = Measure(() => (double)data.Where(x => x % 2 == 0).Select(x => x * 2L).Sum());
    var (plinqB, plinqResB) = Measure(() => (double)data.AsParallel().Where(x => x % 2 == 0).Select(x => x * 2L).Sum());
    PrintRow(size, "Б дешева", linqB, plinqB, linqResB == plinqResB);
}

// Мала колекція: накладні витрати PLINQ
{
    List<int> small = GenerateData(1_000, Seed);
    var (l, _) = Measure(() => small.Where(x => x % 2 == 0).Select(x => x * 2L).Sum(), runs: 50);
    var (p, _) = Measure(() => small.AsParallel().Where(x => x % 2 == 0).Select(x => x * 2L).Sum(), runs: 50);
    Console.WriteLine(new string('-', 88));
    PrintRow(1_000, "Б дешева", l, p, true);
}

// Вплив ступеня паралелізму на дорогу операцію (5M)
Console.WriteLine();
Console.WriteLine("Вплив WithDegreeOfParallelism на операцію А (5 000 000 елементів):");
{
    List<int> data = GenerateData(5_000_000, Seed);
    var (baseline, _) = Measure(() => data.Where(IsPrime).Select(ScoreOf).Sum());
    Console.WriteLine($"  LINQ (1 потік)                  : {baseline,8:F1} мс");
    foreach (int dop in new[] { 1, 2, 4, Environment.ProcessorCount }.Distinct().Where(d => d <= Environment.ProcessorCount))
    {
        var (t, _) = Measure(() => data.AsParallel().WithDegreeOfParallelism(dop).Where(IsPrime).Select(ScoreOf).Sum());
        Console.WriteLine($"  PLINQ, DegreeOfParallelism = {dop,-2} : {t,8:F1} мс  (x{baseline / t:F2})");
    }
}

// =====================================================================
// ЧАСТИНА 2. Побічні ефекти та потокобезпечність
// =====================================================================
Console.WriteLine();
Console.WriteLine("--- Частина 2. Побічні ефекти в PLINQ ---");
{
    List<int> data = GenerateData(5_000_000, Seed);
    int expectedPrimes = data.Count(IsPrime);
    long expectedSum = data.Where(IsPrime).Sum(x => (long)x);
    Console.WriteLine($"Еталон (послідовно): простих чисел = {expectedPrimes:N0}, їх сума = {expectedSum:N0}");
    Console.WriteLine();

    // ---------- 2.1. НЕПРАВИЛЬНО: спільний лічильник і спільна сума без синхронізації ----------
    Console.WriteLine("2.1. НЕПРАВИЛЬНО: count++ та sum += x у ForAll без синхронізації");
    for (int run = 1; run <= 5; run++)
    {
        int count = 0;
        long sum = 0;
        data.AsParallel().Where(IsPrime).ForAll(x =>
        {
            count++;      // read-modify-write: НЕ атомарна операція
            sum += x;     // те саме
        });
        Console.WriteLine($"   запуск {run}: count = {count,9:N0} (втрачено {expectedPrimes - count,7:N0}), " +
                          $"sum = {sum,20:N0}  {(count == expectedPrimes && sum == expectedSum ? "OK" : "ПОМИЛКА")}");
    }

    // ---------- 2.2. НЕПРАВИЛЬНО: List<T>.Add з кількох потоків ----------
    Console.WriteLine();
    Console.WriteLine("2.2. НЕПРАВИЛЬНО: спільний List<int>.Add у Select (побічний ефект усередині запиту)");
    for (int run = 1; run <= 5; run++)
    {
        var primes = new List<int>();
        try
        {
            // Select використовується як "цикл" з побічним ефектом — так робити не можна
            _ = data.AsParallel().Where(IsPrime).Select(x => { primes.Add(x); return x; }).Count();
            Console.WriteLine($"   запуск {run}: у списку {primes.Count,9:N0} елементів " +
                              $"(очікувалось {expectedPrimes:N0})  {(primes.Count == expectedPrimes ? "OK" : "ПОМИЛКА")}");
        }
        catch (AggregateException ex)
        {
            Console.WriteLine($"   запуск {run}: виняток {ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}");
        }
    }

    // ---------- 2.3. ВИПРАВЛЕННЯ ----------
    Console.WriteLine();
    Console.WriteLine("2.3. ВИПРАВЛЕННЯ (по 3 запуски кожного способу):");

    // a) lock
    var sw = Stopwatch.StartNew();
    bool okLock = true;
    for (int run = 0; run < 3; run++)
    {
        int count = 0; long sum = 0;
        object sync = new();
        data.AsParallel().Where(IsPrime).ForAll(x =>
        {
            lock (sync) { count++; sum += x; }
        });
        okLock &= count == expectedPrimes && sum == expectedSum;
    }
    sw.Stop();
    Console.WriteLine($"   a) lock                         : {(okLock ? "OK" : "ПОМИЛКА")}  ({sw.Elapsed.TotalMilliseconds / 3,7:F1} мс/запуск)");

    // b) Interlocked
    sw.Restart();
    bool okInterlocked = true;
    for (int run = 0; run < 3; run++)
    {
        int count = 0; long sum = 0;
        data.AsParallel().Where(IsPrime).ForAll(x =>
        {
            Interlocked.Increment(ref count);
            Interlocked.Add(ref sum, x);
        });
        okInterlocked &= count == expectedPrimes && sum == expectedSum;
    }
    sw.Stop();
    Console.WriteLine($"   b) Interlocked.Increment / Add  : {(okInterlocked ? "OK" : "ПОМИЛКА")}  ({sw.Elapsed.TotalMilliseconds / 3,7:F1} мс/запуск)");

    // c) ConcurrentBag<T> замість List<T>
    sw.Restart();
    bool okBag = true;
    for (int run = 0; run < 3; run++)
    {
        var bag = new ConcurrentBag<int>();
        data.AsParallel().Where(IsPrime).ForAll(bag.Add);
        okBag &= bag.Count == expectedPrimes && bag.Sum(x => (long)x) == expectedSum;
    }
    sw.Stop();
    Console.WriteLine($"   c) ConcurrentBag<int>           : {(okBag ? "OK" : "ПОМИЛКА")}  ({sw.Elapsed.TotalMilliseconds / 3,7:F1} мс/запуск)");

    // d) Найкраще: без побічних ефектів — агрегування виконує сам PLINQ
    sw.Restart();
    bool okPure = true;
    for (int run = 0; run < 3; run++)
    {
        var primes = data.AsParallel().Where(IsPrime);
        int count = primes.Count();
        long sum = primes.Sum(x => (long)x);
        List<int> list = data.AsParallel().AsOrdered().Where(IsPrime).ToList(); // ToList теж потокобезпечний
        okPure &= count == expectedPrimes && sum == expectedSum && list.Count == expectedPrimes;
    }
    sw.Stop();
    Console.WriteLine($"   d) чистий запит Count/Sum/ToList: {(okPure ? "OK" : "ПОМИЛКА")}  ({sw.Elapsed.TotalMilliseconds / 3,7:F1} мс/запуск, 3 запити)");

    // e) Порядок елементів: без AsOrdered PLINQ не гарантує порядок
    Console.WriteLine();
    var seq = data.Where(IsPrime).ToList();
    var completion = new ConcurrentQueue<int>();
    data.AsParallel().Where(IsPrime).ForAll(completion.Enqueue);          // порядок завершення в потоках
    var viaEnumerable = data.Select(x => x).AsParallel().Where(IsPrime).ToList(); // джерело — IEnumerable (chunk-розбиття)
    var ordered = data.Select(x => x).AsParallel().AsOrdered().Where(IsPrime).ToList();
    Console.WriteLine("2.4. Порядок результатів (перші 6 простих чисел):");
    Console.WriteLine($"   LINQ                         : {string.Join(", ", seq.Take(6))}");
    Console.WriteLine($"   PLINQ ForAll                 : {string.Join(", ", completion.Take(6))}  -> {(seq.SequenceEqual(completion) ? "той самий порядок" : "ІНШИЙ порядок")}");
    Console.WriteLine($"   PLINQ ToList (IEnumerable)   : {string.Join(", ", viaEnumerable.Take(6))}  -> {(seq.SequenceEqual(viaEnumerable) ? "той самий порядок" : "ІНШИЙ порядок")}");
    Console.WriteLine($"   PLINQ AsOrdered().ToList()   : {string.Join(", ", ordered.Take(6))}  -> {(seq.SequenceEqual(ordered) ? "той самий порядок" : "ІНШИЙ порядок")}");
}

// =====================================================================
// Допоміжні методи
// =====================================================================

static List<int> GenerateData(int size, int seed)
{
    var rnd = new Random(seed);
    var list = new List<int>(size);
    for (int i = 0; i < size; i++)
        list.Add(rnd.Next(2, MaxValue));
    return list;
}

// Обчислювально інтенсивна перевірка на простоту (перебір непарних дільників до √n)
static bool IsPrime(int n)
{
    if (n < 2) return false;
    if (n % 2 == 0) return n == 2;
    int limit = (int)Math.Sqrt(n);
    for (int d = 3; d <= limit; d += 2)
        if (n % d == 0) return false;
    return true;
}

// Додаткове "складне математичне перетворення"
static double ScoreOf(int n) => Math.Sqrt(n) * Math.Log(n) + Math.Sin(n);

static (double ms, double result) Measure(Func<double> action, int runs = Runs)
{
    double result = action(); // прогрів (JIT, пул потоків)
    double total = 0;
    for (int i = 0; i < runs; i++)
    {
        var sw = Stopwatch.StartNew();
        result = action();
        sw.Stop();
        total += sw.Elapsed.TotalMilliseconds;
    }
    return (total / runs, result);
}

static void PrintRow(int size, string op, double linq, double plinq, bool same)
{
    Console.WriteLine($"{size,12:N0} | {op,9} | {linq,10:F2} | {plinq,10:F2} | {"x" + (linq / plinq).ToString("F2"),11} | {(same ? "так" : "НІ")}");
}
