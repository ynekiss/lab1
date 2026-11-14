namespace IndependentWork11;

// Результати пайплайнів (етап Load — "завантажуємо" у готові до виводу моделі)
public record ProductPopularity(string ProductName, int TotalQuantity);
public record CategoryTop(string Category, List<ProductPopularity> TopProducts);
public record CityRevenue(string City, decimal Revenue, int SalesCount);
public record MonthlySales(int Year, int Month, int TotalQuantity, decimal TotalRevenue);
public record CustomerInsight(string CustomerName, string City, int Purchases, decimal TotalSpent,
                              string FavoriteCategory, decimal AverageCheck);

/// <summary>
/// Етап Transform: усі аналітичні LINQ-пайплайни.
/// Кожен метод повертає матеріалізований список (ToList), щоб вимірювання часу
/// охоплювало реальне виконання запиту, а не лише його побудову (відкладене виконання).
/// </summary>
public class SalesAnalytics
{
    private readonly List<Sale> _sales;
    private readonly List<Product> _products;
    private readonly List<Customer> _customers;

    public SalesAnalytics(List<Sale> sales, List<Product> products, List<Customer> customers)
    {
        _sales = sales;
        _products = products;
        _customers = customers;
    }

    /// <summary>
    /// Пайплайн 1: найпопулярніші продукти за категоріями (топ-N за кількістю).
    /// GroupBy(продукт) → Sum → Join(продукти) → GroupBy(категорія) → OrderByDescending → Take
    /// </summary>
    public List<CategoryTop> TopProductsByCategory(int top = 3)
    {
        return _sales
            .GroupBy(s => s.ProductId)                                  // 1. групуємо продажі за продуктом
            .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(s => s.Quantity) }) // 2. сумарна кількість
            .Join(_products,                                            // 3. додаємо назву та категорію
                  stat => stat.ProductId,
                  p => p.Id,
                  (stat, p) => new { p.Name, p.Category, stat.TotalQty })
            .GroupBy(x => x.Category)                                   // 4. групуємо за категорією
            .Select(cat => new CategoryTop(
                cat.Key,
                cat.OrderByDescending(x => x.TotalQty)                  // 5. топ-3 у кожній категорії
                   .ThenBy(x => x.Name)
                   .Take(top)
                   .Select(x => new ProductPopularity(x.Name, x.TotalQty))
                   .ToList()))
            .OrderBy(c => c.Category)
            .ToList();
    }

    /// <summary>
    /// Пайплайн 2: загальний дохід за містами клієнтів.
    /// Join(продукти) → Join(клієнти) → Select(дохід) → GroupBy(місто) → Sum → OrderByDescending
    /// </summary>
    public List<CityRevenue> RevenueByCity()
    {
        return _sales
            .Join(_products, s => s.ProductId, p => p.Id,
                  (s, p) => new { s.CustomerId, Revenue = s.Quantity * p.UnitPrice })
            .Join(_customers, x => x.CustomerId, c => c.Id,
                  (x, c) => new { c.City, x.Revenue })
            .GroupBy(x => x.City)
            .Select(g => new CityRevenue(g.Key, g.Sum(x => x.Revenue), g.Count()))
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }

    /// <summary>
    /// Пайплайн 2 (варіант для порівняння продуктивності): замість Join — попередньо
    /// побудований словник Id → ціна / місто (ToDictionary), і пошук O(1) у Select.
    /// </summary>
    public List<CityRevenue> RevenueByCityWithDictionaries()
    {
        var priceById = _products.ToDictionary(p => p.Id, p => p.UnitPrice);
        var cityById = _customers.ToDictionary(c => c.Id, c => c.City);

        return _sales
            .GroupBy(s => cityById[s.CustomerId])
            .Select(g => new CityRevenue(g.Key, g.Sum(s => s.Quantity * priceById[s.ProductId]), g.Count()))
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }

    /// <summary>
    /// Пайплайн 3: продажі за роками та місяцями.
    /// Join(продукти) → GroupBy(рік, місяць) → Sum(кількість), Sum(дохід) → OrderBy(рік) → ThenBy(місяць)
    /// </summary>
    public List<MonthlySales> SalesByMonth()
    {
        return _sales
            .Join(_products, s => s.ProductId, p => p.Id,
                  (s, p) => new { s.SaleDate, s.Quantity, Revenue = s.Quantity * p.UnitPrice })
            .GroupBy(x => new { x.SaleDate.Year, x.SaleDate.Month })   // складений ключ (анонімний тип)
            .Select(g => new MonthlySales(
                g.Key.Year,
                g.Key.Month,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.Revenue)))
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();
    }

    /// <summary>
    /// Пайплайн 4 (додатковий): "найцінніші клієнти".
    /// Для кожного клієнта з мінімум minPurchases покупками — загальні витрати, середній чек
    /// та улюблена категорія (за кількістю куплених одиниць). Топ-N за витратами.
    /// Оператори: Join, Join, GroupBy, Where, Select, Aggregate, GroupBy (вкладений), OrderByDescending, Take.
    /// </summary>
    public List<CustomerInsight> TopCustomers(int top = 5, int minPurchases = 3)
    {
        return _sales
            .Join(_products, s => s.ProductId, p => p.Id,
                  (s, p) => new { s.CustomerId, s.Quantity, p.Category, Revenue = s.Quantity * p.UnitPrice })
            .Join(_customers, x => x.CustomerId, c => c.Id,
                  (x, c) => new { Customer = c, x.Quantity, x.Category, x.Revenue })
            .GroupBy(x => x.Customer)
            .Where(g => g.Count() >= minPurchases)
            .Select(g =>
            {
                // Aggregate: одним проходом рахуємо і суму, і кількість покупок
                var totals = g.Aggregate(
                    (Sum: 0m, Count: 0),
                    (acc, x) => (acc.Sum + x.Revenue, acc.Count + 1));

                string favorite = g.GroupBy(x => x.Category)
                                   .OrderByDescending(c => c.Sum(x => x.Quantity))
                                   .ThenBy(c => c.Key)
                                   .First().Key;

                return new CustomerInsight(g.Key.Name, g.Key.City, totals.Count, totals.Sum,
                                           favorite, totals.Sum / totals.Count);
            })
            .OrderByDescending(c => c.TotalSpent)
            .Take(top)
            .ToList();
    }
}
