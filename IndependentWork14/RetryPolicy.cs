using System.Diagnostics;

namespace IndependentWork14;

/// <summary>
/// Політика повторних спроб з експоненційною затримкою (exponential backoff) та випадковим jitter.
/// Затримка перед повтором номер retryAttempt (0, 1, 2, ...):
///   delay = initialDelay * 2^retryAttempt * (1 + random.NextDouble() * jitterFactor * 2 - jitterFactor)
/// тобто базова затримка подвоюється з кожною спробою і випадково змінюється в межах ±jitterFactor.
/// </summary>
public class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _jitterFactor;

    private readonly Random _random;
    private readonly Action<TimeSpan> _sleep;

    /// <param name="maxRetries">Максимальна кількість ПОВТОРНИХ спроб (усього викликів = maxRetries + 1).</param>
    /// <param name="initialDelay">Початкова затримка для першого повтору.</param>
    /// <param name="jitterFactor">Частка випадкового відхилення: 0.2 означає ±20%.</param>
    public RetryPolicy(int maxRetries, TimeSpan initialDelay, double jitterFactor = 0.2)
        : this(maxRetries, initialDelay, jitterFactor, Random.Shared, Thread.Sleep)
    {
    }

    /// <summary>
    /// Розширений конструктор: дозволяє підставити власний Random (фіксований Seed для відтворюваності)
    /// і власну функцію очікування (наприклад, "без сну" у тестах).
    /// </summary>
    public RetryPolicy(int maxRetries, TimeSpan initialDelay, double jitterFactor,
                       Random random, Action<TimeSpan> sleep)
    {
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Кількість повторів не може бути від'ємною.");
        if (initialDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(initialDelay), "Затримка не може бути від'ємною.");
        if (jitterFactor < 0 || jitterFactor >= 1)
            throw new ArgumentOutOfRangeException(nameof(jitterFactor), "jitterFactor має бути в межах [0; 1).");

        _maxRetries = maxRetries;
        _initialDelay = initialDelay;
        _jitterFactor = jitterFactor;
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _sleep = sleep ?? throw new ArgumentNullException(nameof(sleep));
    }

    public int MaxRetries => _maxRetries;

    /// <summary>
    /// Виконує операцію; у разі винятку повторює її з експоненційною затримкою та jitter.
    /// </summary>
    /// <param name="operation">Операція, яку потрібно виконати.</param>
    /// <param name="shouldRetry">
    /// Визначає, чи варто повторювати спробу для конкретного винятку.
    /// null — повторювати для будь-якого винятку.
    /// </param>
    /// <returns>Результат першого успішного виклику операції.</returns>
    /// <exception cref="Exception">
    /// Останній виняток операції — якщо спроби вичерпано або shouldRetry повернув false.
    /// </exception>
    public T Execute<T>(Func<T> operation, Func<Exception, bool> shouldRetry = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var total = Stopwatch.StartNew();

        for (int retryAttempt = 0; ; retryAttempt++)
        {
            int attemptNumber = retryAttempt + 1;
            Log($"Спроба {attemptNumber}/{_maxRetries + 1}...", ConsoleColor.Gray);

            try
            {
                T result = operation();
                Log($"Спроба {attemptNumber}: УСПІХ (загальний час {total.Elapsed.TotalMilliseconds:F0} мс)", ConsoleColor.Green);
                return result;
            }
            catch (Exception ex)
            {
                Log($"Спроба {attemptNumber}: НЕВДАЧА — {ex.GetType().Name}: {ex.Message}", ConsoleColor.Yellow);

                if (shouldRetry != null && !shouldRetry(ex))
                {
                    Log($"Виняток {ex.GetType().Name} не підлягає повтору (shouldRetry = false). Операцію перервано.", ConsoleColor.Red);
                    throw; // "throw;" зберігає оригінальний стек викликів
                }

                if (retryAttempt >= _maxRetries)
                {
                    Log($"Вичерпано всі {_maxRetries} повторних спроб (загальний час {total.Elapsed.TotalMilliseconds:F0} мс). " +
                        "Виняток передається далі.", ConsoleColor.Red);
                    throw;
                }

                TimeSpan delay = CalculateDelay(retryAttempt, out TimeSpan baseDelay, out double multiplier);
                Log($"Очікування {delay.TotalMilliseconds:F0} мс перед повтором " +
                    $"(база {baseDelay.TotalMilliseconds:F0} мс × jitter {multiplier:F3})", ConsoleColor.DarkCyan);
                _sleep(delay);
            }
        }
    }

    /// <summary>
    /// Обчислює затримку за обов'язковою формулою:
    /// initialDelay * Math.Pow(2, retryAttempt) * (1 + random.NextDouble() * jitterFactor * 2 - jitterFactor)
    /// </summary>
    public TimeSpan CalculateDelay(int retryAttempt) => CalculateDelay(retryAttempt, out _, out _);

    private TimeSpan CalculateDelay(int retryAttempt, out TimeSpan baseDelay, out double jitterMultiplier)
    {
        baseDelay = _initialDelay * Math.Pow(2, retryAttempt);
        jitterMultiplier = 1 + _random.NextDouble() * _jitterFactor * 2 - _jitterFactor;
        return baseDelay * jitterMultiplier;
    }

    private static void Log(string message, ConsoleColor color)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] [Retry] {message}");
        Console.ForegroundColor = old;
    }
}
