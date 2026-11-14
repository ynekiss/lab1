# Самостійна робота №19. Звіт: власні винятки та Polly

Предметна область — інтернет-магазин електроніки. Бібліотека — **Polly.Core 8.6.4**, API `ResiliencePipelineBuilder`.

## 1. Власний виняток `OutOfStockException`

```csharp
public class OutOfStockException : Exception
{
    public string Sku { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }
    public int Shortage => Math.Max(0, RequestedQuantity - AvailableQuantity);

    public OutOfStockException() : base("Товару недостатньо на складі.") { }
    public OutOfStockException(string message) : base(message) { }
    public OutOfStockException(string message, Exception innerException) : base(message, innerException) { }

    public OutOfStockException(string sku, int requestedQuantity, int availableQuantity) : ...
    public OutOfStockException(string message, string sku, int requested, int available,
                               Exception innerException = null) : base(message, innerException) { ... }
}
```

Що зроблено:
- три стандартні конструктори і два конструктори з контекстом помилки;
- додаткові властивості, завдяки яким обробник може запропонувати покупцю конкретну дію («замовте 2 шт.»), не розбираючи текст повідомлення;
- `Warehouse.Reserve` кидає `OutOfStockException`, якщо товару не вистачає;
- `OrderService` перехоплює технічний `KeyNotFoundException` і кидає `OutOfStockException` з першопричиною в `InnerException`;
- у `Main` чотири замовлення (успіх, нестача, неіснуючий товар, від'ємна кількість) обробляються блоками `catch` від специфічного до загального, з фільтром `when (ex.InnerException is null)`, а `finally` виконується завжди;
- окремо продемонстровано три стандартні конструктори та метод `TryReserve`, що повертає `Result<int>`.

## 2. Сценарії Polly

Кожна стратегія логує свої події через колбеки (`OnRetry`, `OnOpened`/`OnHalfOpened`/`OnClosed`, `OnTimeout`, `OnFallback`). Імітовані сервіси логують кожен виклик і відповідь, а логер показує час від початку сценарію.

### Сценарій 1. Retry — API служби доставки (`RetryScenario.cs`)

**Проблема:** API перевізника іноді на короткий час повертає 503 або 429. Запит статусу посилки ідемпотентний, тому його безпечно повторювати.

**Стратегія:** Retry, 4 повтори, `Exponential` від 200 мс, `UseJitter = true`. `ShouldHandle` пропускає лише 5xx, 429 і мережеві збої, а 404 (неіснуюча ТТН) не повторюється, бо це постійна помилка.

```csharp
.AddRetry(new RetryStrategyOptions
{
    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(IsTransient),
    MaxRetryAttempts = 4,
    Delay = TimeSpan.FromMilliseconds(200),
    BackoffType = DelayBackoffType.Exponential,
    UseJitter = true,
    OnRetry = args => { Log.Polly($"[Retry] Повтор #{args.AttemptNumber + 1} через {args.RetryDelay.TotalMilliseconds:F0} мс ..."); return default; }
})
```

Реалізовано три випадки: API відповідає після двох збоїв; API недоступне довше, ніж тривають повтори (виняток обробляється в коді сценарію); неіснуюча ТТН (404) без повторів.

### Сценарій 2. Circuit Breaker — сервіс рекомендацій (`CircuitBreakerScenario.cs`)

**Проблема:** сервіс блоку «З цим товаром купують» недоступний кілька секунд. Повтори лише навантажили б його й затримали б сторінку, а рекомендації не критичні.

**Стратегія:** Circuit Breaker. Якщо за 5 с було щонайменше 3 запити й ≥50% з них невдалі, ланцюг розмикається на 1.5 с, і запити відхиляються одразу з `BrokenCircuitException`, без звернення до сервісу. Потім пропускається пробний запит (Half-Open): успіх замикає ланцюг, невдача знову розмикає.

```csharp
.AddCircuitBreaker(new CircuitBreakerStrategyOptions
{
    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutException>(),
    FailureRatio = 0.5,
    MinimumThroughput = 3,
    SamplingDuration = TimeSpan.FromSeconds(5),
    BreakDuration = TimeSpan.FromSeconds(1.5),
    StateProvider = _state,
    OnOpened = ..., OnHalfOpened = ..., OnClosed = ...
})
```

Імітація: 16 запитів кожні 250 мс до сервісу, який перші 3 с відповідає 503. Для кожного запиту в лог пишеться стан вимикача, а в кінці — скільки разів сервіс справді викликався.

### Сценарій 3. Timeout + Retry + Fallback — калькулятор доставки (`TimeoutFallbackScenario.cs`)

**Проблема:** зовнішній калькулятор тарифів іноді не падає, а зависає на секунди, і користувач чекає на спінер у кошику.

**Стратегія** (від зовнішньої до внутрішньої):
- **Fallback** — якщо нічого не вдалося, показується стандартний тариф 120 грн;
- **Retry** — 2 повтори з фіксованою затримкою 100 мс, бо зависання часто одноразове;
- **Timeout** — кожна спроба обмежена 300 мс (`TimeoutRejectedException`). Сервіс використовує `Task.Delay(delay, ct)`, тому тайм-аут справді перериває очікування.

```csharp
new ResiliencePipelineBuilder<decimal>()
    .AddFallback(new FallbackStrategyOptions<decimal> { FallbackAction = _ => Outcome.FromResultAsValueTask(120m), ... })
    .AddRetry(new RetryStrategyOptions<decimal> { MaxRetryAttempts = 2, Delay = TimeSpan.FromMilliseconds(100), ... })
    .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMilliseconds(300), ... })
    .Build();
```

Реалізовано три випадки: перша спроба зависає, друга швидка; калькулятор зависає щоразу (спрацьовує Fallback); калькулятор відповідає одразу.

## 3. Коли кидати виняток, а коли повертати `Result<T>`

Виняток доречний, коли метод не може виконати свій контракт і ситуація справді виняткова: зник файл, розірвано з'єднання з БД, некоректний аргумент. Його неможливо випадково проігнорувати, і він несе стек викликів. Тому `Warehouse.Reserve` під час оформлення замовлення кидає `OutOfStockException`: кошик уже перевірено, і нестача означає, що товар купили паралельно. `Result<T>` доречний для очікуваних неуспіхів, які є нормальною частиною логіки: валідація, пошук, що може нічого не знайти, перевірка наявності під час додавання в кошик (`TryReserve`). Він швидший, видний у сигнатурі методу, але його результат можна забути перевірити. Правило: якщо невдача — один із нормальних варіантів відповіді, повертаємо `Result`; якщо щось пішло не так, як мало, — кидаємо виняток.

## 4. Висновки щодо Polly

**Переваги:**
- стратегії описуються конфігурацією, а не ручними циклами з `try/catch` і `Thread.Sleep`; бізнес-код нічого не знає про повтори;
- Retry, Timeout, Circuit Breaker і Fallback легко комбінуються в одному пайплайні, а порядок стратегій явно задає поведінку;
- готові перевірені реалізації jitter, Half-Open і скасування через `CancellationToken`;
- колбеки дають точки для логування й метрик; є інтеграція з `HttpClientFactory` і `ILogger`.

**Обмеження:**
- Polly не робить операцію ідемпотентною, і повтор неідемпотентних операцій може дати дублі;
- неправильні параметри шкодять: надто агресивний Retry або вкладені повтори перевантажують сервіс;
- `Timeout` працює лише з кодом, що підтримує `CancellationToken`;
- Circuit Breaker має стан, тому пайплайн треба створювати один раз і перевикористовувати;
- Polly маскує нестабільність, але не усуває її причин, тому потрібен моніторинг.

## 5. Відповіді на контрольні запитання

1. **Виняток проти Result:** виняток — коли метод не може виконати контракт (`File.ReadAllText` для відсутнього файлу, `Warehouse.Reserve`); Result — для очікуваних неуспіхів (`int.TryParse`, `Warehouse.TryReserve`).
2. **Три стандартні конструктори:** без параметрів; `(string message)`; `(string message, Exception innerException)`.
3. **Circuit Breaker проти Retry:** Retry повторює операцію при короткому збої, Circuit Breaker після серії збоїв на час перестає викликати сервіс і відхиляє запити одразу. Retry — для коротких тимчасових збоїв, Circuit Breaker — для тривалих відмов. Їх часто комбінують.
4. **Bulkhead** обмежує кількість одночасних операцій до ресурсу, щоб повільний сервіс не зайняв усі потоки чи з'єднання застосунку. У Polly v8 це Concurrency/Rate Limiter (`Polly.RateLimiting`).
5. **Навіщо jitter:** без нього клієнти, які зіткнулися зі збоєм одночасно, повторюють запити теж одночасно й знову перевантажують сервіс.
6. **Ризики Retry для неідемпотентних операцій:** якщо перша спроба виконалася, а загубилася лише відповідь, повтор виконає дію вдруге (подвійна оплата, дубль замовлення). Потрібні ключі ідемпотентності або відмова від повторів.
