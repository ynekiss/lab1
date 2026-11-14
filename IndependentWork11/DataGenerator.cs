namespace IndependentWork11;

/// <summary>
/// Етап Extract: генерує псевдовипадкові, але відтворювані дані (фіксований Seed).
/// </summary>
public static class DataGenerator
{
    private static readonly Dictionary<string, string[]> CategoryProducts = new()
    {
        ["Електроніка"] = new[] { "Ноутбук", "Смартфон", "Планшет", "Навушники", "Монітор", "Миша", "Клавіатура", "Павербанк", "Колонка", "Вебкамера", "Роутер", "SSD-диск", "Флешка" },
        ["Побутова техніка"] = new[] { "Чайник", "Мікрохвильовка", "Пилосос", "Праска", "Блендер", "Кавоварка", "Тостер", "Фен", "Мультиварка", "Обігрівач", "Вентилятор", "Міксер" },
        ["Книги"] = new[] { "Кобзар", "Тигролови", "Місто", "Захар Беркут", "Лісова пісня", "Intermezzo", "Чорна рада", "Кайдашева сім'я", "Clean Code", "C# in Depth", "CLR via C#", "Design Patterns" },
        ["Одяг"] = new[] { "Футболка", "Джинси", "Куртка", "Светр", "Кросівки", "Шапка", "Шарф", "Худі", "Сорочка", "Шорти", "Рукавиці", "Кепка", "Вишиванка" },
        ["Спорт"] = new[] { "М'яч", "Гантелі", "Скакалка", "Килимок для йоги", "Велосипед", "Ракетка", "Еспандер", "Самокат", "Рюкзак", "Пляшка для води", "Ролики", "Турнік" },
        ["Продукти"] = new[] { "Кава зернова", "Чай зелений", "Мед", "Шоколад", "Горіхи", "Оливкова олія", "Сир твердий", "Печиво", "Варення", "Гранола", "Сухофрукти", "Спеції", "Макарони" }
    };

    private static readonly string[] Cities =
    {
        "Рівне", "Київ", "Львів", "Луцьк", "Житомир", "Тернопіль", "Хмельницький", "Одеса", "Харків", "Дніпро"
    };

    private static readonly string[] FirstNames =
    {
        "Олександр", "Марія", "Андрій", "Олена", "Дмитро", "Ірина", "Сергій", "Наталія", "Іван", "Юлія",
        "Максим", "Анна", "Богдан", "Софія", "Тарас", "Катерина", "Віктор", "Оксана", "Роман", "Дарина"
    };

    private static readonly string[] LastNames =
    {
        "Шевченко", "Коваленко", "Бондаренко", "Ткаченко", "Кравченко", "Мельник", "Олійник", "Поліщук", "Лисенко", "Савченко"
    };

    public static List<Product> GenerateProducts(Random rnd)
    {
        var products = new List<Product>();
        int id = 1;
        foreach (var (category, names) in CategoryProducts)
        {
            foreach (var name in names)
            {
                decimal basePrice = category switch
                {
                    "Електроніка" => 500m + rnd.Next(0, 40000),
                    "Побутова техніка" => 400m + rnd.Next(0, 8000),
                    "Книги" => 150m + rnd.Next(0, 900),
                    "Одяг" => 200m + rnd.Next(0, 3000),
                    "Спорт" => 150m + rnd.Next(0, 9000),
                    _ => 40m + rnd.Next(0, 600)
                };
                products.Add(new Product { Id = id++, Name = name, Category = category, UnitPrice = basePrice });
            }
        }
        return products; // 75 продуктів
    }

    public static List<Customer> GenerateCustomers(Random rnd, int count = 100)
    {
        var customers = new List<Customer>(count);
        for (int i = 1; i <= count; i++)
        {
            customers.Add(new Customer
            {
                Id = i,
                Name = $"{FirstNames[rnd.Next(FirstNames.Length)]} {LastNames[rnd.Next(LastNames.Length)]}",
                City = Cities[rnd.Next(Cities.Length)]
            });
        }
        return customers;
    }

    public static List<Sale> GenerateSales(Random rnd, List<Product> products, List<Customer> customers, int count)
    {
        var sales = new List<Sale>(count);
        var start = new DateTime(2024, 1, 1);
        int daysRange = (new DateTime(2025, 12, 31) - start).Days;

        for (int i = 1; i <= count; i++)
        {
            // Нерівномірний розподіл: продукти з меншим Id "популярніші" (квадрат випадкового числа)
            double r = rnd.NextDouble();
            int productIndex = (int)(r * r * products.Count);

            sales.Add(new Sale
            {
                Id = i,
                ProductId = products[productIndex].Id,
                CustomerId = customers[rnd.Next(customers.Count)].Id,
                Quantity = rnd.Next(1, 11),
                SaleDate = start.AddDays(rnd.Next(daysRange + 1)).AddMinutes(rnd.Next(24 * 60))
            });
        }
        return sales;
    }
}
