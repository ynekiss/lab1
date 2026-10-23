namespace IndependentWork2
{
    /// <summary>
    /// Товар інтернет-магазину з трьома перевантаженими конструкторами.
    /// </summary>
    public class Product
    {
        // Приватні поля
        private readonly int _id;
        private readonly string _name;
        private readonly decimal _price;
        private readonly string _category;
        private readonly int _stockCount;

        // Read-only властивості: значення можна прочитати, але не змінити після створення
        public int Id { get { return _id; } }
        public string Name { get { return _name; } }
        public decimal Price { get { return _price; } }
        public string Category { get { return _category; } }
        public int StockCount { get { return _stockCount; } }

        /// <summary>
        /// Конструктор 1 (основний): приймає всі п'ять параметрів.
        /// Уся ініціалізація полів зосереджена тільки тут.
        /// </summary>
        public Product(int id, string name, decimal price, string category, int stockCount)
        {
            _id = id;
            _name = name;
            _price = price;
            _category = category;
            _stockCount = stockCount;
        }

        /// <summary>
        /// Конструктор 2 (швидке додавання): тільки id, назва і ціна.
        /// Викликає основний конструктор через : this(...).
        /// </summary>
        public Product(int id, string name, decimal price)
            : this(id, name, price, "Uncategorized", 0)
        {
        }

        /// <summary>
        /// Конструктор 3 (копіювання): створює дублікат іншого товару.
        /// Також викликає основний конструктор через : this(...).
        /// </summary>
        public Product(Product other)
            : this(other.Id, other.Name, other.Price, other.Category, other.StockCount)
        {
        }

        public override string ToString()
        {
            return $"ID: {Id}, Name: {Name}, Price: {Price:C}, Category: {Category}, Stock: {StockCount}";
        }
    }
}
