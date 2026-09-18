using System;

namespace Lab2
{
    // Клас згідно з варіантом Figure
    public class Figure
    {
        // Приватні поля
        private string _name;
        private string _color;
        private double _area;

        // Публічні властивості
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Color
        {
            get => _color;
            set => _color = value;
        }

        // Властивість з валідацією (площа не може бути від'ємною)
        public double Area
        {
            get => _area;
            set
            {
                if (value < 0)
                {
                    Console.WriteLine($"[Помилка]: Площа для {Name} не може бути від'ємною ({value}). Встановлено 0.0.");
                    _area = 0.0;
                }
                else
                {
                    _area = value;
                }
            }
        }

        // Конструктор за замовчуванням (викликає параметризований через : this())
        public Figure() : this("Circle", "Red", 0.0)
        {
            Console.WriteLine("--> Викликано конструктор за замовчуванням");
        }

        // Параметризований конструктор
        public Figure(string name, string color, double area)
        {
            _name = name;
            _color = color;
            Area = area; // Проходить через валідацію у set
            Console.WriteLine($"--> Викликано параметризований конструктор для {Name}");
        }

        // Метод, що повертає інформацію про фігуру
        public string GetFigureInfo()
        {
            return $"Фігура: {Name} | Колір: {Color} | Площа: {Area} кв. од.";
        }

        // Деструктор (фіналізатор)
        ~Figure()
        {
            Console.WriteLine($"[GC]: Деструктор викликано. Об'єкт '{_name}' знищено з пам'яті.");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Creating objects ===");

            // 1. Створення об'єкта через конструктор за замовчуванням
            Figure fig1 = new Figure();
            
            // 2. Створення об'єкта через параметризований конструктор
            Figure fig2 = new Figure("Square", "Blue", 25.5);

            // 3. Створення об'єкта з від'ємною площею для перевірки валідації
            Figure fig3 = new Figure("Triangle", "Green", -12.0);

            Console.WriteLine("\n=== Displaying Info ===");
            Console.WriteLine(fig1.GetFigureInfo());
            Console.WriteLine(fig2.GetFigureInfo());
            Console.WriteLine(fig3.GetFigureInfo());

            Console.WriteLine("\n=== Objects created ===");
            Console.WriteLine("=== End of Main, preparing for GC ===");

            // Обнуляємо посилання, щоб GC вважав об'єкти сміттям
            fig1 = null;
            fig2 = null;
            fig3 = null;

            // Примусовий виклик збирача сміття (для навчальних цілей)
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}