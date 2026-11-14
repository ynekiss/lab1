# Самостійна робота №19

**Тема:** Дизайн винятків і політики retry: теорія та кейси Polly.

**Мета:** Опрацювати теорію обробки винятків у .NET, вивчити патерни відмовостійкості (Retry, Circuit Breaker, Bulkhead) та бібліотеку Polly, реалізувати практичні сценарії.

**Хід роботи:**
1. Опрацював теоретичний матеріал: `try-catch-finally`, власні винятки, виняток проти `Result`, Retry з exponential backoff і jitter, Circuit Breaker, Bulkhead, API Polly v8.
2. Створив консольний проєкт `IndependentWork19` і підключив пакет `Polly.Core` 8.6.4.
3. Обрав предметну область «інтернет-магазин електроніки» і створив власний виняток `OutOfStockException` (`Domain/OutOfStockException.cs`): три стандартні конструктори, конструктори з контекстом і властивості `Sku`, `RequestedQuantity`, `AvailableQuantity`, `Shortage`.
4. Створив клас `Warehouse`: метод `Reserve` кидає виняток, метод `TryReserve` повертає `Result<T>`. Клас `OrderService` обгортає `KeyNotFoundException` в `OutOfStockException` через `innerException`. У `Main` продемонстрував `try` з кількома `catch` (від специфічного до загального, з фільтром `when`) і `finally`.
5. Через `ResiliencePipelineBuilder` реалізував три сценарії Polly з логуванням кожної спроби:
   - `RetryScenario` — Retry з exponential backoff і jitter для API служби доставки (повторюються лише 5xx/429, 404 — ні);
   - `CircuitBreakerScenario` — Circuit Breaker для сервісу рекомендацій, який тимчасово недоступний;
   - `TimeoutFallbackScenario` — Timeout + Retry + Fallback для калькулятора вартості доставки, який зависає.
6. Описав сценарії, обґрунтування стратегій, виняток проти `Result` і висновки в [REPORT.md](REPORT.md).

**Висновок:** Власні винятки з додатковими властивостями дозволяють обробнику реагувати на помилку по суті, а `Result<T>` зручніший для очікуваних неуспіхів. Polly дозволяє декларативно описати й комбінувати стратегії відмовостійкості, але не робить операції ідемпотентними, тому параметри повторів треба підбирати свідомо.

**Запуск:** `dotnet run`
