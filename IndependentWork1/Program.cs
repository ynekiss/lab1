using System.Globalization;
using System.Text;

namespace IndependentWork1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA"); // українське форматування чисел і дат

            Console.WriteLine("=== Самостійна робота №1: класи Thermostat, GymMembership, Flight ===");
            Console.WriteLine();

            // 1. Термостат
            Console.WriteLine("--- Thermostat ---");
            Thermostat thermostat = new Thermostat("Вітальня", 18.0, 22.0);
            thermostat.PrintInfo();
            thermostat.Simulate(25);
            Console.WriteLine("Через 25 хвилин:");
            thermostat.PrintInfo();
            thermostat.Simulate(30);
            Console.WriteLine("Ще через 30 хвилин:");
            thermostat.PrintInfo();
            thermostat.TargetTemperature = 19.5;
            Console.WriteLine("Змінили цільову температуру на 19.5 °C:");
            thermostat.PrintInfo();
            try
            {
                thermostat.TargetTemperature = 45;
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("Спроба встановити 45 °C відхилена властивістю TargetTemperature.");
            }
            Console.WriteLine();

            // 2. Абонемент у спортзал
            Console.WriteLine("--- GymMembership ---");
            DateTime start = new DateTime(2026, 10, 1);
            GymMembership membership = new GymMembership("Олена Коваль", start, 30);
            DateTime today = new DateTime(2026, 10, 22);

            membership.Visit(new DateTime(2026, 10, 2));
            membership.Visit(new DateTime(2026, 10, 5));
            membership.Visit(today);
            bool lateVisit = membership.Visit(new DateTime(2026, 11, 15));

            Console.WriteLine($"Власник: {membership.OwnerName}");
            Console.WriteLine($"Діє до: {membership.EndDate:dd.MM.yyyy}");
            Console.WriteLine($"Активний на {today:dd.MM.yyyy}: {(membership.IsActive(today) ? "так" : "ні")}");
            Console.WriteLine($"Залишилось днів: {membership.DaysLeft(today)}");
            Console.WriteLine($"Відвідувань: {membership.Visits}");
            Console.WriteLine($"Відвідування 15.11.2026 зараховано: {(lateVisit ? "так" : "ні (абонемент закінчився)")}");
            Console.WriteLine();

            // 3. Авіарейс
            Console.WriteLine("--- Flight ---");
            Flight flight = new Flight("PS-101", "Варшава", 180, 3200m);
            Console.WriteLine($"Рейс {flight.Number} до {flight.Destination}, вільних місць: {flight.FreeSeats}");
            Console.WriteLine($"Бронювання 120 місць: {(flight.BookSeats(120) ? "успішно" : "відмова")}");
            Console.WriteLine($"Бронювання 70 місць: {(flight.BookSeats(70) ? "успішно" : "відмова (не вистачає місць)")}");
            Console.WriteLine($"Бронювання 45 місць: {(flight.BookSeats(45) ? "успішно" : "відмова")}");
            Console.WriteLine($"Вільних місць: {flight.FreeSeats}");
            Console.WriteLine($"Заповненість: {flight.GetOccupancyPercent():F1} %");
            Console.WriteLine($"Виручка: {flight.GetRevenue():N2} грн");
        }
    }
}
