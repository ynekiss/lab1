// Самостійна робота №19. Дизайн винятків і політики retry: теорія та кейси Polly.
// Проєкт IndependentWork19. Звіт — у файлі REPORT.md.
//
// Предметна область: інтернет-магазин електроніки.
//   Частина 1 — власний виняток OutOfStockException + try/catch/finally + Result<T>.
//   Частина 2 — три сценарії відмовостійкості з Polly (ResiliencePipelineBuilder, Polly.Core 8.x):
//     1) Retry + exponential backoff + jitter  — API служби доставки;
//     2) Circuit Breaker                        — сервіс рекомендацій;
//     3) Timeout + Retry + Fallback             — калькулятор вартості доставки.

using System.Text;
using IndependentWork19.Domain;
using IndependentWork19.Scenarios;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("=== Самостійна робота №19: власні винятки та Polly ===");

// =====================================================================
// ЧАСТИНА 1. Власний тип винятку
// =====================================================================
Log.Header("ЧАСТИНА 1. Власний виняток OutOfStockException");

var warehouse = new Warehouse();
var orders = new OrderService(warehouse);

// Набір замовлень, які викликають різні ситуації
var attempts = new (int OrderId, string Sku, int Qty)[]
{
    (1001, "LAPTOP-15", 2),    // успіх
    (1002, "MONITOR-27", 5),   // OutOfStockException (на складі 2)
    (1003, "TABLET-10", 1),    // KeyNotFoundException → обгорнуто в OutOfStockException (InnerException)
    (1004, "MOUSE-01", -3)     // ArgumentOutOfRangeException
};

foreach (var (orderId, sku, qty) in attempts)
{
    Console.WriteLine();
    Console.WriteLine($"Замовлення #{orderId}: {sku} × {qty}");
    try
    {
        orders.PlaceOrder(orderId, sku, qty);
        Console.WriteLine($"  ✔ Зарезервовано. Залишок на складі: {warehouse.GetStock(sku)} шт.");
    }
    catch (OutOfStockException ex) when (ex.InnerException is null)   // найспецифічніший випадок — першим
    {
        Console.WriteLine($"  ✘ OutOfStockException: {ex.Message}");
        Console.WriteLine($"    Sku = {ex.Sku}, RequestedQuantity = {ex.RequestedQuantity}, " +
                          $"AvailableQuantity = {ex.AvailableQuantity}, Shortage = {ex.Shortage}");
        Console.WriteLine($"    → Пропонуємо покупцю замовити {ex.AvailableQuantity} шт. або підписатися на надходження.");
    }
    catch (OutOfStockException ex)
    {
        Console.WriteLine($"  ✘ OutOfStockException: {ex.Message}");
        Console.WriteLine($"    InnerException: {ex.InnerException.GetType().Name} — {ex.InnerException.Message}");
    }
    catch (ArgumentException ex)       // ArgumentOutOfRangeException успадковує ArgumentException
    {
        Console.WriteLine($"  ✘ {ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" ")}");
    }
    catch (Exception ex)               // найзагальніший — останнім
    {
        Console.WriteLine($"  ✘ Непередбачена помилка {ex.GetType().Name}: {ex.Message}");
    }
    finally
    {
        Console.WriteLine($"  (finally) Обробку замовлення #{orderId} завершено, запис у журнал аудиту.");
    }
}

// Демонстрація трьох стандартних конструкторів
Console.WriteLine();
Console.WriteLine("Три стандартні конструктори:");
var e1 = new OutOfStockException();
var e2 = new OutOfStockException("Склад тимчасово заблоковано для інвентаризації.");
var e3 = new OutOfStockException("Не вдалося перевірити залишки.", new TimeoutException("БД складу не відповіла"));
Console.WriteLine($"  ()                       → \"{e1.Message}\"");
Console.WriteLine($"  (message)                → \"{e2.Message}\"");
Console.WriteLine($"  (message, inner)         → \"{e3.Message}\" / Inner: {e3.InnerException?.GetType().Name}");

// Той самий випадок через Result<T> — для очікуваних неуспіхів
Console.WriteLine();
Console.WriteLine("Той самий склад, але метод TryReserve повертає Result<int> (додавання в кошик):");
foreach (var (sku, qty) in new[] { ("MOUSE-01", 3), ("MONITOR-27", 5), ("TABLET-10", 1) })
{
    Result<int> r = warehouse.TryReserve(sku, qty);
    Console.WriteLine(r.IsSuccess
        ? $"  {sku} × {qty}: {r} → у кошику, залишок {r.Value} шт."
        : $"  {sku} × {qty}: {r} → показуємо підказку біля кнопки «Купити»");
}

// =====================================================================
// ЧАСТИНА 2. Polly
// =====================================================================
await new RetryScenario().RunAsync();
await new CircuitBreakerScenario().RunAsync();
await new TimeoutFallbackScenario().RunAsync();

Console.WriteLine();
Console.WriteLine("Готово.");
