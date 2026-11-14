using System.Diagnostics;
using System.Text;

namespace lab12v20;

// ===================== RetryHelper =====================

public static class RetryHelper
{
    private static readonly TimeSpan DefaultInitialDelay = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Виконує операцію і при винятку повторює її з експоненційною затримкою:
    /// delay = initialDelay * 2^retryAttempt (retryAttempt = 0, 1, 2, ...).
    /// </summary>
    /// <param name="operation">Операція, яку потрібно виконати.</param>
    /// <param name="retryCount">Максимальна кількість ПОВТОРНИХ спроб (усього викликів = retryCount + 1).</param>
    /// <param name="initialDelay">Початкова затримка; default означає 200 мс.</param>
    /// <param name="shouldRetry">Чи повторювати для цього винятку; null — повторювати для будь-якого.</param>
    public static T ExecuteWithRetry<T>(
        Func<T> operation,
        int retryCount = 3,
        TimeSpan initialDelay = default,
        Func<Exception, bool>? shouldRetry = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (retryCount < 0)
            throw new ArgumentOutOfRangeException(nameof(retryCount), "Кількість повторів не може бути від'ємною.");
        if (initialDelay == default)
            initialDelay = DefaultInitialDelay;

        var total = Stopwatch.StartNew();

        for (int retryAttempt = 0; ; retryAttempt++)
        {
            int attemptNumber = retryAttempt + 1;
            try
            {
                Log($"Спроба {attemptNumber}/{retryCount + 1}...");
                T result = operation();
                Log($"Спроба {attemptNumber}: успіх (усього {total.ElapsedMilliseconds} мс)", ConsoleColor.Green);
                return result;
            }
            catch (Exception ex)
            {
                Log($"Спроба {attemptNumber}: {ex.GetType().Name} — {ex.Message}", ConsoleColor.Yellow);

                if (shouldRetry != null && !shouldRetry(ex))
                {
                    Log($"shouldRetry = false для {ex.GetType().Name}: повтор не має сенсу, виняток передається далі", ConsoleColor.Red);
                    throw;
                }

                if (retryAttempt >= retryCount)
                {
                    Log($"Усі {retryCount} повторні спроби вичерпано, виняток передається далі", ConsoleColor.Red);
                    throw;
                }

                TimeSpan delay = initialDelay * Math.Pow(2, retryAttempt);
                Log($"Очікування {delay.TotalMilliseconds:F0} мс ({initialDelay.TotalMilliseconds:F0} мс × 2^{retryAttempt})", ConsoleColor.DarkCyan);
                Thread.Sleep(delay);
            }
        }
    }

    private static void Log(string message, ConsoleColor color = ConsoleColor.Gray)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] [Retry] {message}");
        Console.ForegroundColor = old;
    }
}

// ===================== FileProcessor (варіант 20) =====================

/// <summary>
/// Імітує читання шаблону push-сповіщення з файлу.
/// Перші failuresBeforeSuccess викликів файл "заблоковано" іншим процесом (IOException).
/// </summary>
public class FileProcessor
{
    private readonly int _failuresBeforeSuccess;
    public int Calls { get; private set; }

    public FileProcessor(int failuresBeforeSuccess = 3)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public string GetNotificationPayload(string path)
    {
        Calls++;

        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException($"Файл шаблону не знайдено: {path}", path);

        if (Calls <= _failuresBeforeSuccess)
            throw new IOException($"Файл {path} заблоковано іншим процесом (виклик #{Calls})");

        return "{ \"title\": \"Замовлення відправлено\", \"body\": \"Ваша посилка вже в дорозі\" }";
    }
}

// ===================== NetworkClient (варіант 20) =====================

/// <summary>
/// Імітує відправлення push-сповіщення на пристрій.
/// Перші failuresBeforeSuccess викликів сервер повертає помилку (HttpRequestException).
/// </summary>
public class NetworkClient
{
    private readonly int _failuresBeforeSuccess;
    public int Calls { get; private set; }
    public List<string> Delivered { get; } = new();

    public NetworkClient(int failuresBeforeSuccess = 2)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public void SendPushNotification(string deviceId, string payload)
    {
        Calls++;

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Ідентифікатор пристрою не може бути порожнім.", nameof(deviceId));

        if (Calls <= _failuresBeforeSuccess)
            throw new HttpRequestException($"503 Service Unavailable: push-сервер перевантажений (виклик #{Calls})");

        Delivered.Add(deviceId);
    }
}

// ===================== Програма =====================

public class Program
{
    // shouldRetry за варіантом: повторювати для IOException та HttpRequestException
    private static bool IsTransient(Exception ex) => ex is IOException or HttpRequestException;

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var initialDelay = TimeSpan.FromMilliseconds(200);

        // ---------- Сценарій 1: читання payload, IOException перші 3 рази ----------
        Console.WriteLine("=== Сценарій 1. FileProcessor.GetNotificationPayload: IOException перші 3 рази ===");
        var fileProcessor = new FileProcessor(failuresBeforeSuccess: 3);
        string payload = RetryHelper.ExecuteWithRetry(
            () => fileProcessor.GetNotificationPayload("templates/order-shipped.json"),
            retryCount: 3,
            initialDelay: initialDelay,
            shouldRetry: IsTransient);
        Console.WriteLine($"Результат: {payload}");
        Console.WriteLine($"Викликів FileProcessor: {fileProcessor.Calls}");
        Console.WriteLine();

        // ---------- Сценарій 2: відправлення push, HttpRequestException перші 2 рази ----------
        Console.WriteLine("=== Сценарій 2. NetworkClient.SendPushNotification: HttpRequestException перші 2 рази ===");
        var networkClient = new NetworkClient(failuresBeforeSuccess: 2);
        // SendPushNotification повертає void, тому обгортаємо його у Func<bool>
        bool sent = RetryHelper.ExecuteWithRetry(
            () => { networkClient.SendPushNotification("device-7f3a91", payload); return true; },
            retryCount: 3,
            initialDelay: initialDelay,
            shouldRetry: IsTransient);
        Console.WriteLine($"Результат: сповіщення відправлено = {sent}, отримувачі: {string.Join(", ", networkClient.Delivered)}");
        Console.WriteLine($"Викликів NetworkClient: {networkClient.Calls}");
        Console.WriteLine();

        // ---------- Сценарій 3: shouldRetry не дозволяє повтор (ArgumentException) ----------
        Console.WriteLine("=== Сценарій 3. shouldRetry: порожній deviceId (ArgumentException не повторюється) ===");
        var badClient = new NetworkClient(failuresBeforeSuccess: 0);
        try
        {
            RetryHelper.ExecuteWithRetry(
                () => { badClient.SendPushNotification("", payload); return true; },
                retryCount: 3,
                initialDelay: initialDelay,
                shouldRetry: IsTransient);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Перехоплено в Main: {ex.GetType().Name}: {ex.Message}");
        }
        Console.WriteLine($"Викликів NetworkClient: {badClient.Calls} (без повторів)");
        Console.WriteLine();

        // ---------- Сценарій 4: уточнений shouldRetry — FileNotFoundException не повторюємо ----------
        Console.WriteLine("=== Сценарій 4. Уточнений shouldRetry: FileNotFoundException (нащадок IOException) не повторюється ===");
        var missingFile = new FileProcessor(failuresBeforeSuccess: 0);
        try
        {
            RetryHelper.ExecuteWithRetry(
                () => missingFile.GetNotificationPayload("templates/order-shipped.txt"),
                retryCount: 3,
                initialDelay: initialDelay,
                shouldRetry: ex => ex is FileNotFoundException ? false : IsTransient(ex));
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Перехоплено в Main: {ex.GetType().Name}: {ex.Message}");
        }
        Console.WriteLine($"Викликів FileProcessor: {missingFile.Calls} (без повторів)");
        Console.WriteLine();

        // ---------- Сценарій 5: сервер недоступний довше, ніж тривають повтори ----------
        Console.WriteLine("=== Сценарій 5. Push-сервер недоступний: усі спроби невдалі ===");
        var downClient = new NetworkClient(failuresBeforeSuccess: int.MaxValue);
        try
        {
            RetryHelper.ExecuteWithRetry(
                () => { downClient.SendPushNotification("device-7f3a91", payload); return true; },
                retryCount: 3,
                initialDelay: initialDelay,
                shouldRetry: IsTransient);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Перехоплено в Main: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine("Сповіщення буде поставлено в чергу для повторного відправлення пізніше.");
        }
        finally
        {
            Console.WriteLine($"Викликів NetworkClient: {downClient.Calls}");
        }
    }
}

/*
 * Контрольні запитання
 *
 * 1. Файли: FileNotFoundException, DirectoryNotFoundException, IOException (файл заблоковано,
 *    диск зайнятий), UnauthorizedAccessException. Мережа: HttpRequestException, SocketException,
 *    TimeoutException / TaskCanceledException.
 * 2. Retry повторює операцію, яка впала через тимчасову помилку, з паузою між спробами.
 *    Доцільний для тимчасових збоїв (заблокований файл, перевантажений сервер) і для ідемпотентних
 *    операцій; недоцільний для постійних помилок (немає файлу, неправильні аргументи).
 * 3. Затримка перед повтором номер k: initialDelay * 2^k. Тут initialDelay = 200 мс,
 *    тож паузи 200, 400, 800 мс.
 * 4. shouldRetry відокремлює тимчасові помилки, які варто повторювати (IOException,
 *    HttpRequestException), від постійних, які треба одразу повернути викликачу
 *    (ArgumentException, FileNotFoundException).
 */
