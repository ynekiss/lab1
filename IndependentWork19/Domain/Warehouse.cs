namespace IndependentWork19.Domain;

/// <summary>
/// Склад інтернет-магазину. Демонструє обидва підходи до сигналізації про помилки:
/// виняток (Reserve) і Result (TryReserve).
/// </summary>
public class Warehouse
{
    private readonly Dictionary<string, int> _stock = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LAPTOP-15"] = 5,
        ["MOUSE-01"] = 120,
        ["MONITOR-27"] = 2
    };

    public int GetStock(string sku) => _stock.TryGetValue(sku, out int qty) ? qty : 0;

    /// <summary>
    /// Резервування на етапі ОФОРМЛЕННЯ замовлення. Кошик уже перевірено, тож нестача тут —
    /// виняткова ситуація (хтось купив товар паралельно): метод не може виконати свій контракт → виняток.
    /// </summary>
    public void Reserve(string sku, int quantity)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("Артикул не може бути порожнім.", nameof(sku));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Кількість має бути більшою за 0.");
        if (!_stock.ContainsKey(sku))
            throw new KeyNotFoundException($"Товар з артикулом {sku} не існує.");

        int available = _stock[sku];
        if (available < quantity)
            throw new OutOfStockException(sku, quantity, available);

        _stock[sku] = available - quantity;
    }

    /// <summary>
    /// Перевірка під час ДОДАВАННЯ в кошик. Відсутність товару тут — звичайна, очікувана ситуація,
    /// тому повертаємо Result замість винятку.
    /// </summary>
    public Result<int> TryReserve(string sku, int quantity)
    {
        if (quantity <= 0)
            return Result<int>.Failure("Кількість має бути більшою за 0.");

        int available = GetStock(sku);
        if (available == 0)
            return Result<int>.Failure($"Товару {sku} немає в наявності.");
        if (available < quantity)
            return Result<int>.Failure($"Можна замовити не більше {available} шт. товару {sku}.");

        _stock[sku] = available - quantity;
        return Result<int>.Success(available - quantity);
    }
}

/// <summary>Сервіс оформлення замовлень: обгортає низькорівневі винятки (innerException).</summary>
public class OrderService
{
    private readonly Warehouse _warehouse;

    public OrderService(Warehouse warehouse) => _warehouse = warehouse;

    public void PlaceOrder(int orderId, string sku, int quantity)
    {
        try
        {
            _warehouse.Reserve(sku, quantity);
        }
        catch (KeyNotFoundException ex)
        {
            // Перетворюємо технічний виняток на виняток предметної області,
            // зберігаючи першопричину в InnerException.
            throw new OutOfStockException($"Замовлення #{orderId}: товар {sku} відсутній у каталозі.",
                                          sku, quantity, 0, ex);
        }
    }
}
