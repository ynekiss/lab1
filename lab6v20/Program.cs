using System;

namespace lab6v20
{
    public class PaymentMethod
    {
        private string _name;
        private bool _isActive;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; }
        }

        public PaymentMethod(string name, bool isActive)
        {
            _name = name;
            _isActive = isActive;
        }

        public virtual void ProcessPayment(decimal amount)
        {
            if (IsActive)
                Console.WriteLine($"[PaymentMethod] Платіж {amount} грн оброблено методом \"{Name}\".");
            else
                Console.WriteLine($"[PaymentMethod] Метод \"{Name}\" неактивний.");
        }

        public string GetMethodType()
        {
            return "Загальний метод оплати";
        }
    }

    public class CreditCard : PaymentMethod
    {
        private string _cardNumber;
        private string _expiryDate;

        public string CardNumber
        {
            get { return _cardNumber; }
            set { _cardNumber = value; }
        }

        public string ExpiryDate
        {
            get { return _expiryDate; }
            set { _expiryDate = value; }
        }

        public CreditCard(string name, bool isActive, string cardNumber, string expiryDate)
            : base(name, isActive)
        {
            _cardNumber = cardNumber;
            _expiryDate = expiryDate;
        }

        public override void ProcessPayment(decimal amount)
        {
            if (IsActive)
                Console.WriteLine($"[CreditCard] Оплачено {amount} грн карткою {CardNumber} (дійсна до {ExpiryDate}).");
            else
                Console.WriteLine($"[CreditCard] Картка {CardNumber} неактивна.");
        }

        public void AuthorizeTransaction()
        {
            Console.WriteLine($"[CreditCard] Транзакцію для картки {CardNumber} авторизовано.");
        }

        public new string GetMethodType()
        {
            return "Кредитна картка";
        }
    }

    public class PayPal : PaymentMethod
    {
        private string _email;

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public PayPal(string name, bool isActive, string email)
            : base(name, isActive)
        {
            _email = email;
        }

        public override void ProcessPayment(decimal amount)
        {
            if (IsActive)
                Console.WriteLine($"[PayPal] Оплачено {amount} грн через PayPal-акаунт {Email}.");
            else
                Console.WriteLine($"[PayPal] Акаунт {Email} неактивний.");
        }

        public void SendInvoice()
        {
            Console.WriteLine($"[PayPal] Рахунок (invoice) надіслано на {Email}.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== Лабораторна робота №6 (Варіант 20) ===");
            Console.WriteLine("Студент: Рибонька Єгор\n");

            Console.WriteLine("=== 1. Створення об'єктів ===");
            PaymentMethod cash = new PaymentMethod("Готівка", true);
            CreditCard card = new CreditCard("Visa", true, "4111-XXXX-XXXX-1111", "12/28");
            PayPal payPal = new PayPal("PayPal", true, "rybonka@example.com");
            Console.WriteLine($"Створено: {cash.Name}, {card.Name}, {payPal.Name}");

            Console.WriteLine("\n=== 2. Унікальні методи похідних класів ===");
            card.AuthorizeTransaction();
            payPal.SendInvoice();

            Console.WriteLine("\n=== 3. Поліморфізм (virtual / override) ===");
            PaymentMethod[] methods = { cash, card, payPal };
            foreach (PaymentMethod method in methods)
            {
                method.ProcessPayment(250.75m);
            }

            Console.WriteLine("\n=== 4. Різниця між override та new ===");
            CreditCard cardRef = new CreditCard("MasterCard", true, "5500-XXXX-XXXX-2222", "10/27");
            PaymentMethod baseRef = cardRef; 

            Console.WriteLine("override — викликається метод реального об'єкта (CreditCard):");
            Console.WriteLine("  через посилання CreditCard:");
            cardRef.ProcessPayment(100m);
            Console.WriteLine("  через посилання PaymentMethod:");
            baseRef.ProcessPayment(100m);

            Console.WriteLine("\nnew — викликається метод за типом посилання:");
            Console.WriteLine($"  через посилання CreditCard:    {cardRef.GetMethodType()}");
            Console.WriteLine($"  через посилання PaymentMethod: {baseRef.GetMethodType()}");
        }
    }
}
