namespace IndependentWork19.Domain;

/// <summary>
/// Власний виняток предметної області "Інтернет-магазин / склад":
/// замовлено більше одиниць товару, ніж є на складі.
/// </summary>
public class OutOfStockException : Exception
{
    /// <summary>Артикул (SKU) товару.</summary>
    public string Sku { get; }

    /// <summary>Скільки одиниць запросив покупець.</summary>
    public int RequestedQuantity { get; }

    /// <summary>Скільки одиниць фактично є на складі.</summary>
    public int AvailableQuantity { get; }

    /// <summary>Скільки одиниць не вистачає.</summary>
    public int Shortage => Math.Max(0, RequestedQuantity - AvailableQuantity);

    // --- три стандартні конструктори ---

    public OutOfStockException()
        : base("Товару недостатньо на складі.")
    {
    }

    public OutOfStockException(string message)
        : base(message)
    {
    }

    public OutOfStockException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    // --- конструктор з контекстом помилки ---

    public OutOfStockException(string sku, int requestedQuantity, int availableQuantity)
        : this($"Товар {sku}: запитано {requestedQuantity} шт., на складі лише {availableQuantity} шт.",
               sku, requestedQuantity, availableQuantity)
    {
    }

    public OutOfStockException(string message, string sku, int requestedQuantity, int availableQuantity,
                               Exception innerException = null)
        : base(message, innerException)
    {
        Sku = sku;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}
