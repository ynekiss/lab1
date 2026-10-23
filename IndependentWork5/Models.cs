namespace IndependentWork5
{
    public class Product
    {
        public string Name { get; }
        public decimal Price { get; }

        public Product(string name, decimal price)
        {
            Name = name;
            Price = price;
        }

        public override string ToString() => $"Product: {Name}, ціна {Price:N2} грн";
    }

    public class Customer
    {
        public string FullName { get; }
        public string Email { get; }

        public Customer(string fullName, string email)
        {
            FullName = fullName;
            Email = email;
        }

        public override string ToString() => $"Customer: {FullName} <{Email}>";
    }

    public class Order
    {
        public int Number { get; }
        public Customer Customer { get; }
        public List<Product> Items { get; }

        public Order(int number, Customer customer, List<Product> items)
        {
            Number = number;
            Customer = customer;
            Items = items;
        }

        public decimal Total => Items.Sum(p => p.Price);

        public override string ToString() =>
            $"Order #{Number}: {Customer.FullName}, товарів: {Items.Count}, сума {Total:N2} грн";
    }

    // Клас БЕЗ перевизначення ToString() - для порівняння
    public class Warehouse
    {
        public string City { get; } = "Рівне";
    }
}
