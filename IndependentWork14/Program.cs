// Самостійна робота №20. Реалізація exponential backoff.
// Проєкт IndependentWork14. Висновки — у файлі REPORT.md.
//
// Обрані параметри: maxRetries = 4, initialDelay = 200 мс, jitterFactor = 0.25 (±25%).
// Сценарій: клієнт сервісу курсів валют, який періодично перевантажений.

using System.Text;
using IndependentWork14;

Console.OutputEncoding = Encoding.UTF8;

var policy = new RetryPolicy(maxRetries: 4, initialDelay: TimeSpan.FromMilliseconds(200), jitterFactor: 0.25);

// Повторюємо лише ТИМЧАСОВІ збої: мережа, тайм-аут, перевантаження сервера.
// Помилки авторизації чи некоректні аргументи повтор не виправить.
static bool IsTransient(Exception ex) =>
    ex is HttpRequestException or TimeoutException or ServiceBusyException;

Console.WriteLine("=== Самостійна робота №20: RetryPolicy з exponential backoff та jitter ===");
Console.WriteLine("Параметри: maxRetries = 4, initialDelay = 200 мс, jitterFactor = 0.25");
Console.WriteLine();

// ---------------------------------------------------------------------
// 0. Як змінюється затримка (без виконання операцій)
// ---------------------------------------------------------------------
Console.WriteLine("--- 0. Розрахунок затримок за формулою (5 вибірок для кожного повтору) ---");
Console.WriteLine($"{"Повтор",7} | {"База, мс",9} | {"Діапазон ±25%, мс",19} | Фактичні затримки, мс");
for (int attempt = 0; attempt < 4; attempt++)
{
    double baseMs = 200 * Math.Pow(2, attempt);
    var samples = Enumerable.Range(0, 5).Select(_ => policy.CalculateDelay(attempt).TotalMilliseconds.ToString("F0"));
    Console.WriteLine($"{attempt + 1,7} | {baseMs,9:F0} | {baseMs * 0.75,8:F0} – {baseMs * 1.25,-8:F0} | {string.Join(", ", samples)}");
}
Console.WriteLine();

// ---------------------------------------------------------------------
// 1. Операція спочатку падає, а потім успішна
// ---------------------------------------------------------------------
Console.WriteLine("--- Сценарій 1: сервіс курсів валют відповідає з 4-ї спроби ---");
var flakyService = new ExchangeRateService(failuresBeforeSuccess: 3);
try
{
    decimal rate = policy.Execute(() => flakyService.GetRate("USD"), IsTransient);
    Console.WriteLine($"РЕЗУЛЬТАТ: курс USD = {rate} грн (усього викликів сервісу: {flakyService.Calls})");
}
catch (Exception ex)
{
    Console.WriteLine($"РЕЗУЛЬТАТ: невдача — {ex.Message}");
}
Console.WriteLine();

// ---------------------------------------------------------------------
// 2. Операція завжди падає — виняток після вичерпання спроб
// ---------------------------------------------------------------------
Console.WriteLine("--- Сценарій 2: сервіс недоступний постійно ---");
var deadService = new ExchangeRateService(failuresBeforeSuccess: int.MaxValue);
try
{
    decimal rate = policy.Execute(() => deadService.GetRate("EUR"), IsTransient);
    Console.WriteLine($"РЕЗУЛЬТАТ: курс EUR = {rate}");
}
catch (Exception ex)
{
    Console.WriteLine($"РЕЗУЛЬТАТ: перехоплено {ex.GetType().Name} після {deadService.Calls} викликів: {ex.Message}");
}
Console.WriteLine();

// ---------------------------------------------------------------------
// 3. shouldRetry: помилка, яку немає сенсу повторювати
// ---------------------------------------------------------------------
Console.WriteLine("--- Сценарій 3: неправильний API-ключ (shouldRetry повертає false) ---");
var unauthorizedService = new ExchangeRateService(failuresBeforeSuccess: 0, apiKeyValid: false);
try
{
    decimal rate = policy.Execute(() => unauthorizedService.GetRate("PLN"), IsTransient);
    Console.WriteLine($"РЕЗУЛЬТАТ: курс PLN = {rate}");
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"РЕЗУЛЬТАТ: {ex.GetType().Name} без повторів (викликів: {unauthorizedService.Calls}): {ex.Message}");
}
Console.WriteLine();

// ---------------------------------------------------------------------
// 4. Узагальненість: та сама політика для операції з іншим типом результату
// ---------------------------------------------------------------------
Console.WriteLine("--- Сценарій 4: Execute<string> — читання конфігурації з 2-ї спроби, shouldRetry = null ---");
int configCalls = 0;
string config = policy.Execute(() =>
{
    configCalls++;
    if (configCalls < 2)
        throw new IOException("Файл config.json заблоковано іншим процесом");
    return "{ \"currency\": \"UAH\", \"refreshMinutes\": 15 }";
});
Console.WriteLine($"РЕЗУЛЬТАТ: {config}");

// =====================================================================
// Імітація зовнішнього сервісу
// =====================================================================

/// <summary>Сервіс перевантажений — тимчасовий збій.</summary>
public class ServiceBusyException : Exception
{
    public ServiceBusyException() { }
    public ServiceBusyException(string message) : base(message) { }
    public ServiceBusyException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Імітує зовнішній API курсів валют: перші failuresBeforeSuccess викликів завершуються
/// тимчасовими помилками різних типів, далі — успіх.
/// </summary>
public class ExchangeRateService
{
    private readonly int _failuresBeforeSuccess;
    private readonly bool _apiKeyValid;
    private static readonly Dictionary<string, decimal> Rates = new()
    {
        ["USD"] = 41.37m, ["EUR"] = 48.02m, ["PLN"] = 11.29m
    };

    public int Calls { get; private set; }

    public ExchangeRateService(int failuresBeforeSuccess, bool apiKeyValid = true)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
        _apiKeyValid = apiKeyValid;
    }

    public decimal GetRate(string currency)
    {
        Calls++;

        if (!_apiKeyValid)
            throw new UnauthorizedAccessException("401 Unauthorized: недійсний API-ключ");

        if (Calls <= _failuresBeforeSuccess)
        {
            throw (Calls % 3) switch
            {
                1 => new HttpRequestException("503 Service Unavailable"),
                2 => new TimeoutException("Сервер не відповів за 2 с"),
                _ => new ServiceBusyException("429 Too Many Requests")
            };
        }

        return Rates[currency];
    }
}
