using System;

namespace lab6v20
{
    public class PaymentMethod
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public PaymentMethod(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }

        public virtual void ProcessPayment(decimal amount)
        {
            if (IsActive)
            {
                Console.WriteLine($"[PaymentMethod] Обробка платежу {amount:C} через базовий метод: {Name}.");
            }
            else
            {
                Console.WriteLine($"[PaymentMethod] Метод оплати {Name} неактивний.");
            }
        }

        public string GetPaymentType()
        {
            return "General Payment Method";
        }
    }

    public class CreditCard : PaymentMethod
    {
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }

        public CreditCard(string name, bool isActive, string cardNumber, string expiryDate)
            : base(name, isActive)
        {
            CardNumber = cardNumber;
            ExpiryDate = expiryDate;
        }

        public override void ProcessPayment(decimal amount)
        {
            if (IsActive)
            {
                Console.WriteLine($"[CreditCard] Оплачено {amount:C} карткою {CardNumber} (Дійсна до: {ExpiryDate}).");
            }
            else
            {
                Console.WriteLine($"[CreditCard] Картка {CardNumber} заблокована або неактивна.");
            }
        }

        public void AuthorizeTransaction()
        {
            Console.WriteLine($"[CreditCard] Авторизація транзакції для картки {CardNumber} пройшла успішно.");
        }

        public new string GetPaymentType()
        {
            return "Credit Card Payment";
        }
    }

    public class PayPal : PaymentMethod
    {
        public string Email { get; set; }

        public PayPal(string name, bool isActive, string email)
            : base(name, isActive)
        {
            Email = email;
        }

        public override void ProcessPayment(decimal amount)
        {
            if (IsActive)
            {
                Console.WriteLine($"[PayPal] Оплачено {amount:C} через PayPal акаунт ({Email}).");
            }
            else
            {
                Console.WriteLine($"[PayPal] Акаунт {Email} неактивний.");
            }
        }

        public void SendInvoice()
        {
            Console.WriteLine($"[PayPal] Надіслано інвойс на електронну пошту {Email}.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== Створення об'єктів ===");
            PaymentMethod generalPayment = new PaymentMethod("Готівка", true);
            CreditCard creditCard = new CreditCard("Visa Classic", true, "4111-XXXX-XXXX-1111", "12/28");
            PayPal payPal = new PayPal("PayPal Account", true, "rybonka@example.com");

            Console.WriteLine("\n=== Виклик унікальних методів ===");
            creditCard.AuthorizeTransaction();
            payPal.SendInvoice();

            Console.WriteLine("\n=== Демонстрація поліморфізму (override) ===");
          
            PaymentMethod[] paymentMethods = new PaymentMethod[] { generalPayment, creditCard, payPal };

            foreach (var method in paymentMethods)
            {
                
                method.ProcessPayment(150.50m);
            }

            Console.WriteLine("\n=== Демонстрація різниці між override та new ===");
            CreditCard explicitCard = new CreditCard("MasterCard", true, "5500-XXXX-XXXX-2222", "10/27");
            PaymentMethod implicitCard = explicitCard; 

            Console.WriteLine("Виклик GetPaymentType() через посилання типу CreditCard:");
            Console.WriteLine($"Тип: {explicitCard.GetPaymentType()}"); 

            Console.WriteLine("\nВиклик GetPaymentType() через посилання типу PaymentMethod (new не забезпечує поліморфізму):");
            Console.WriteLine($"Тип: {implicitCard.GetPaymentType()}"); 

            Console.WriteLine("\nАле метод ProcessPayment() (override) працює поліморфно в обох випадках:");
            explicitCard.ProcessPayment(500.00m);
            implicitCard.ProcessPayment(500.00m);
        }
    }
}