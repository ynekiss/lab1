namespace IndependentWork11;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public decimal UnitPrice { get; set; }

    public override string ToString() => $"[{Id}] {Name} ({Category}, {UnitPrice:F2} грн)";
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }

    public override string ToString() => $"[{Id}] {Name} ({City})";
}

public class Sale
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime SaleDate { get; set; }
    public int CustomerId { get; set; }
}
