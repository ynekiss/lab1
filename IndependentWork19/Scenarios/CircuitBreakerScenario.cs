using Polly;
using Polly.CircuitBreaker;

namespace IndependentWork19.Scenarios;

/// <summary>
/// СЦЕНАРІЙ 2. CIRCUIT BREAKER (автоматичний вимикач).
///
/// Проблема: сторінка товару звертається до сервісу рекомендацій ("З цим товаром купують").
/// Сервіс "впав" на ~3 секунди (оновлення, проблеми з БД). Кожен запит до нього чекає й падає,
/// а кількість запитів лише заважає йому відновитися. Повторювати (Retry) тут недоцільно — збій
/// не миттєвий, а рекомендації не є критичними: сторінку можна показати і без них.
///
/// Стратегія: Circuit Breaker. Якщо в межах 5 с щонайменше 3 запити і ≥50% з них невдалі —
/// "розмикаємо" ланцюг на 1.5 с: запити відхиляються МИТТЄВО (BrokenCircuitException), сервіс
/// не навантажується. Потім Half-Open: один пробний запит; успіх → Closed, невдача → знову Open.
/// </summary>
public class CircuitBreakerScenario
{
    private readonly ResiliencePipeline _pipeline;
    private readonly CircuitBreakerStateProvider _state = new();

    public CircuitBreakerScenario()
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutException>(),
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromSeconds(1.5),
                StateProvider = _state,
                OnOpened = args =>
                {
                    Log.Polly($"[CircuitBreaker] ⚡ OPEN на {args.BreakDuration.TotalSeconds:F1} с " +
                              $"(остання помилка: {args.Outcome.Exception?.Message})");
                    return default;
                },
                OnHalfOpened = _ =>
                {
                    Log.Polly("[CircuitBreaker] HALF-OPEN: пропускаємо пробний запит");
                    return default;
                },
                OnClosed = _ =>
                {
                    Log.Polly("[CircuitBreaker] CLOSED: сервіс відновився, запити йдуть у звичайному режимі");
                    return default;
                }
            })
            .Build();
    }

    public async Task RunAsync()
    {
        Log.Header("СЦЕНАРІЙ 2. Circuit Breaker — сервіс рекомендацій недоступний ~3 с");
        Log.Restart();

        var service = new RecommendationService(downFor: TimeSpan.FromSeconds(3));
        int shown = 0, failed = 0, rejected = 0;

        // 16 запитів від відвідувачів сторінки, кожні 250 мс
        for (int i = 1; i <= 16; i++)
        {
            try
            {
                var items = await _pipeline.ExecuteAsync(async ct => await service.GetRecommendationsAsync("LAPTOP-15", ct));
                shown++;
                Log.Success($"Запит {i,2} [{_state.CircuitState}]: показано рекомендації: {string.Join(", ", items)}");
            }
            catch (BrokenCircuitException)
            {
                rejected++;
                Log.Warn($"Запит {i,2} [{_state.CircuitState}]: відхилено миттєво (ланцюг розімкнено) → сторінка без рекомендацій");
            }
            catch (HttpRequestException ex)
            {
                failed++;
                Log.Error($"Запит {i,2} [{_state.CircuitState}]: помилка сервісу ({ex.Message}) → сторінка без рекомендацій");
            }

            await Task.Delay(250);
        }

        Console.WriteLine();
        Console.WriteLine($"  Підсумок: запитів 16; успішних {shown}; помилок від сервісу {failed}; " +
                          $"відхилено вимикачем без звернення до сервісу {rejected}.");
        Console.WriteLine($"  Фактичних звернень до сервісу рекомендацій: {service.Calls} (без Circuit Breaker було б 16).");
    }

    /// <summary>Імітація сервісу, який недоступний певний час від першого звернення.</summary>
    private sealed class RecommendationService
    {
        private readonly TimeSpan _downFor;
        private DateTime? _firstCall;
        public int Calls { get; private set; }

        public RecommendationService(TimeSpan downFor) => _downFor = downFor;

        public async Task<string[]> GetRecommendationsAsync(string sku, CancellationToken ct)
        {
            Calls++;
            _firstCall ??= DateTime.UtcNow;
            await Task.Delay(40, ct);

            if (DateTime.UtcNow - _firstCall < _downFor)
                throw new HttpRequestException("503 Service Unavailable");

            return new[] { "MOUSE-01", "BAG-15", "USB-HUB" };
        }
    }
}
