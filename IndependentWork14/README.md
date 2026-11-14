# Самостійна робота №20

**Тема:** Реалізація exponential backoff.

**Мета:** Розробити власний допоміжний клас для реалізації патерну Retry з експоненційною затримкою та випадковим jitter.

**Хід роботи:**
1. Створив консольний проєкт `IndependentWork14`.
2. Реалізував клас `RetryPolicy` (`RetryPolicy.cs`):
   - приватні поля `_maxRetries`, `_initialDelay`, `_jitterFactor`;
   - конструктор `RetryPolicy(int maxRetries, TimeSpan initialDelay, double jitterFactor = 0.2)` з перевіркою аргументів;
   - узагальнений метод `T Execute<T>(Func<T> operation, Func<Exception, bool> shouldRetry = null)`, який повторює операцію із затримкою за формулою `initialDelay * Math.Pow(2, retryAttempt) * (1 + random.NextDouble() * jitterFactor * 2 - jitterFactor)`, логує кожну спробу, затримку й причину невдачі, а після вичерпання спроб прокидає останній виняток через `throw;`;
   - `shouldRetry` дозволяє одразу зупинитися на помилці, яку немає сенсу повторювати.
3. Обрав параметри `maxRetries = 4`, `initialDelay = 200 мс`, `jitterFactor = 0.25` і сценарій «клієнт сервісу курсів валют» (`ExchangeRateService` імітує помилки 503, тайм-аут і 429).
4. У `Main` продемонстрував:
   - успіх після кількох невдалих спроб;
   - остаточну невдачу з винятком після всіх спроб;
   - помилку 401, яку `shouldRetry` не дозволяє повторювати;
   - роботу `Execute<string>` з іншим типом результату;
   - розрахунок затримок за формулою.
5. Висновки щодо меж застосування Retry записав у [REPORT.md](REPORT.md).

**Висновок:** Retry з exponential backoff і jitter підходить лише для тимчасових збоїв та ідемпотентних операцій. Для постійних помилок повтор марний, тому потрібен `shouldRetry`, а для тривалих відмов сервісу — Circuit Breaker.

**Запуск:** `dotnet run`
