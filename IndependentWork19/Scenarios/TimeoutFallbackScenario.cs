using Polly;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;

namespace IndependentWork19.Scenarios;

/// <summary>
/// СЦЕНАРІЙ 3. TIMEOUT + RETRY + FALLBACK (комбінація стратегій у типізованому пайплайні).
///
/// Проблема: у кошику показується вартість доставки, яку рахує зовнішній калькулятор тарифів.
/// Іноді він "зависає" на секунди (не падає, а просто довго не відповідає). Без обмеження часу
/// користувач дивиться на спінер, а потік/з'єднання зайняті.
///
/// Стратегія (зовнішня → внутрішня):
///   Fallback  — якщо нічого не вийшло, показуємо стандартний тариф 120 грн (з позначкою "орієнтовно");
///   Retry     — 2 повтори з фіксованою затримкою 100 мс (зависання часто одноразове);
///   Timeout   — кожна окрема спроба обмежена 300 мс (кидає TimeoutRejectedException).
/// Загальний час у найгіршому випадку: 3 × 300 + 2 × 100 ≈ 1.1 с замість невизначеного очікування.
/// </summary>
public class TimeoutFallbackScenario
{
    private const decimal DefaultShippingCost = 120m;
    private readonly ResiliencePipeline<decimal> _pipeline;

    public TimeoutFallbackScenario()
    {
        _pipeline = new ResiliencePipelineBuilder<decimal>()
            .AddFallback(new FallbackStrategyOptions<decimal>
            {
                ShouldHandle = new PredicateBuilder<decimal>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<HttpRequestException>(),
                FallbackAction = _ => Outcome.FromResultAsValueTask(DefaultShippingCost),
                OnFallback = args =>
                {
                    Log.Polly($"[Fallback] Використано стандартний тариф {DefaultShippingCost} грн " +
                              $"(причина: {args.Outcome.Exception?.GetType().Name})");
                    return default;
                }
            })
            .AddRetry(new RetryStrategyOptions<decimal>
            {
                ShouldHandle = new PredicateBuilder<decimal>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<HttpRequestException>(),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    Log.Polly($"[Retry] Повтор #{args.AttemptNumber + 1} через {args.RetryDelay.TotalMilliseconds:F0} мс " +
                              $"({args.Outcome.Exception?.GetType().Name})");
                    return default;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMilliseconds(300),
                OnTimeout = args =>
                {
                    Log.Polly($"[Timeout] Спроба перервана: відповіді немає довше {args.Timeout.TotalMilliseconds:F0} мс");
                    return default;
                }
            })
            .Build();
    }

    public async Task RunAsync()
    {
        Log.Header("СЦЕНАРІЙ 3. Timeout + Retry + Fallback — калькулятор вартості доставки");

        Console.WriteLine("3a) Перша спроба зависає на 2 с, друга відповідає за 80 мс:");
        await CalculateAsync(new ShippingCalculator(2000, 80, 80));

        Console.WriteLine();
        Console.WriteLine("3b) Калькулятор зависає щоразу (2 с) — спрацьовує Fallback:");
        await CalculateAsync(new ShippingCalculator(2000, 2000, 2000));

        Console.WriteLine();
        Console.WriteLine("3c) Калькулятор відповідає одразу — стратегії не втручаються:");
        await CalculateAsync(new ShippingCalculator(60));
    }

    private async Task CalculateAsync(ShippingCalculator calculator)
    {
        Log.Restart();
        var started = DateTime.UtcNow;
        decimal cost = await _pipeline.ExecuteAsync(async ct => await calculator.GetCostAsync("Рівне", 2.4, ct));
        var elapsed = DateTime.UtcNow - started;
        Log.Success($"РЕЗУЛЬТАТ: вартість доставки {cost} грн; спроб: {calculator.Calls}; " +
                    $"час для користувача: {elapsed.TotalMilliseconds:F0} мс");
    }

    /// <summary>Імітація повільного калькулятора: затримка для кожного виклику задається масивом.</summary>
    private sealed class ShippingCalculator
    {
        private readonly int[] _delaysMs;
        public int Calls { get; private set; }

        public ShippingCalculator(params int[] delaysMs) => _delaysMs = delaysMs;

        public async Task<decimal> GetCostAsync(string city, double weightKg, CancellationToken ct)
        {
            int delay = _delaysMs[Math.Min(Calls, _delaysMs.Length - 1)];
            Calls++;
            Log.Info($"→ Калькулятор: {city}, {weightKg} кг (виклик #{Calls}, відповідь займе {delay} мс)");

            // Task.Delay з токеном: коли Timeout скасовує спробу, очікування переривається.
            await Task.Delay(delay, ct);

            decimal cost = 70m + (decimal)weightKg * 15m;
            Log.Info($"← Калькулятор: {cost} грн");
            return cost;
        }
    }
}
