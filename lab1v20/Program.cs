using System;

namespace Lab1
{
    // Варіант 20: Клас Figure
    public class Figure
    {
        // Приватне поле
        private string name;

        // Публічні властивості
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public double Area { get; set; }

        // Конструктор
        public Figure(string name, double area)
        {
            this.name = name;
            Area = area;
        }

        // Деструктор
        ~Figure()
        {
            Console.WriteLine($"Об'єкт '{name}' знищено.");
        }

        // Метод, що повертає інформацію про фігуру
        public string GetFigure()
        {
            return $"Фігура: {Name}, Площа: {Area}";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Створення 3 об'єктів
            Figure fig1 = new Figure("Коло", 78.5);
            Figure fig2 = new Figure("Квадрат", 16.0);
            Figure fig3 = new Figure("Трикутник", 12.5);

            // Виклик методів та вивід у консоль
            Console.WriteLine(fig1.GetFigure());
            Console.WriteLine(fig2.GetFigure());
            Console.WriteLine(fig3.GetFigure());
        }
    }
}