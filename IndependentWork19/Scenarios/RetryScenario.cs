using System.Net;
using Polly;
using Polly.Retry;

namespace IndependentWork19.Scenarios;

/// <summary>
/// СЦЕНАРІЙ 1. RETRY з експоненційною затримкою та jitter.
///
/// Проблема: магазин запитує статус посилки в API служби доставки. API періодично повертає
/// 503 Service Unavailable / 429 Too Many Requests на кілька сотень мілісекунд (перезапуск
/// інстансу, піки навантаження). Помилка тимчасова, а запит статусу — ідемпотентний (GET),
/// тому повторити його безпечно.
///
/// Стратегія: Retry, 4 повтори, Exponential backoff від 200 мс + jitter.
/// Повторюємо ТІЛЬКИ 5xx/429 і мережеві збої; 404 (неіснуючий ТТН) — постійна помилка, без повторів.
/// </summary>
public class RetryScenario
{
    private readonly ResiliencePipeline _pipeline;

    public RetryScenario()
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>(IsTransient),
                MaxRetryAttempts = 4,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    Log.Polly($"[Retry] Повтор #{args.AttemptNumber + 1} через {args.RetryDelay.TotalMilliseconds:F0} мс " +
                              $"(причина: {args.Outcome.Exception?.Message})");
                    return default;
                }
            })
            .Build();
    }

    private static bool IsTransient(HttpRequestException ex) =>
        ex.StatusCode is null                                  // мережевий збій, відповіді немає
        || ex.StatusCode == HttpStatusCode.TooManyRequests      // 429
        || (int)ex.StatusCode >= 500;                           // 5xx

    public async Task RunAsync()
    {
        Log.Header("СЦЕНАРІЙ 1. Retry + Exponential backoff + Jitter — API служби доставки");

        Console.WriteLine("1a) API збоїть двічі (503, 429), потім відповідає:");
        Log.Restart();
        var api = new DeliveryTrackingApi(failuresBeforeSuccess: 2);
        try
        {
            string status = await _pipeline.ExecuteAsync(async ct => await api.GetStatusAsync("20450012345678", ct));
            Log.Success($"РЕЗУЛЬТАТ: «{status}» (викликів API: {api.Calls})");
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"Остаточна невдача: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("1b) API недоступне довше, ніж триває Retry (всі 5 спроб — 503):");
        Log.Restart();
        var downApi = new DeliveryTrackingApi(failuresBeforeSuccess: 100);
        try
        {
            string status = await _pipeline.ExecuteAsync(async ct => await downApi.GetStatusAsync("20450012345678", ct));
            Log.Success($"РЕЗУЛЬТАТ: {status}");
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"Остаточна невдача після {downApi.Calls} викликів: {ex.Message}. " +
                      "Користувачу: «Статус тимчасово недоступний, спробуйте пізніше».");
        }

        Console.WriteLine();
        Console.WriteLine("1c) Неіснуюча ТТН — 404 Not Found не повторюється (ShouldHandle = false):");
        Log.Restart();
        var notFoundApi = new DeliveryTrackingApi(failuresBeforeSuccess: 0);
        try
        {
            await _pipeline.ExecuteAsync(async ct => await notFoundApi.GetStatusAsync("00000000000000", ct));
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"Виняток без повторів (викликів API: {notFoundApi.Calls}): {ex.Message}");
        }
    }

    /// <summary>Імітація зовнішнього API служби доставки.</summary>
    private sealed class DeliveryTrackingApi
    {
        private readonly int _failuresBeforeSuccess;
        public int Calls { get; private set; }

        public DeliveryTrackingApi(int failuresBeforeSuccess) => _failuresBeforeSuccess = failuresBeforeSuccess;

        public async Task<string> GetStatusAsync(string ttn, CancellationToken ct)
        {
            Calls++;
            Log.Info($"→ GET /tracking/{ttn} (виклик #{Calls})");
            await Task.Delay(30, ct); // мережа

            if (ttn.All(c => c == '0'))
                throw new HttpRequestException("404 Not Found: ТТН не знайдено", null, HttpStatusCode.NotFound);

            if (Calls <= _failuresBeforeSuccess)
            {
                var code = Calls % 2 == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.TooManyRequests;
                Log.Warn($"← {(int)code} {code}");
                throw new HttpRequestException($"{(int)code} {code}", null, code);
            }

            Log.Info("← 200 OK");
            return "Посилка прибула у відділення №12, м. Рівне";
        }
    }
}
