using System;

namespace Lab3
{
    // Клас GPSTracker, що реалізує IDisposable та паттерн Dispose
    public class GPSTracker : IDisposable
    {
        private bool _disposed = false;
        private bool _isTracking;
        private readonly string _deviceId;

        public string DeviceId => _deviceId;
        public bool IsTracking => _isTracking;

        public GPSTracker(string deviceId)
        {
            _deviceId = deviceId;
            _isTracking = true;
            Console.WriteLine($"[TRACKER {_deviceId}] GPS Трекер активовано. Відстеження розпочато.");
        }

        public string GetCurrentLocation()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GPSTracker), "Неможливо отримати локацію: об'єкт вже знищено.");
            }

            if (!_isTracking)
            {
                return $"[TRACKER {_deviceId}] Відстеження зупинено.";
            }

            // Імітація отримання координат
            return $"[TRACKER {_deviceId}] Локація: 50.6199° N, 26.2516° E (м. Рівне)";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Звільнення керованих ресурсів (якщо є)
                    Console.WriteLine($"[DISPOSE] Звільнення керованих ресурсів для пристрою {_deviceId}.");
                }

                // Звільнення некерованих ресурсів (імітація зупинки відстеження/закриття порту)
                if (_isTracking)
                {
                    Console.WriteLine($"[DISPOSE] Зупинка GPS-відстеження для пристрою {_deviceId}.");
                    _isTracking = false;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Повідомляємо GC, що фіналізатор викликати не потрібно
        }

        ~GPSTracker()
        {
            Console.WriteLine($"[FINALIZER] Деструктор викликано для пристрою {_deviceId}.");
            Dispose(false);
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== Сценарій 1: Використання конструкції using ===");
            using (var tracker1 = new GPSTracker("DEV-101"))
            {
                Console.WriteLine(tracker1.GetCurrentLocation());
            } // Dispose() викликається автоматично при виході з блоку using

            Console.WriteLine("\n=== Сценарій 2: Явний виклик Dispose() ===");
            var tracker2 = new GPSTracker("DEV-102");
            Console.WriteLine(tracker2.GetCurrentLocation());
            tracker2.Dispose(); // Явне звільнення ресурсів

            Console.WriteLine("\n=== Сценарій 3: Робота деструктора через GC.Collect() ===");
            CreateAndAbandonTracker();

            // Примусовий виклик GC для демонстрації фіналізації
            Console.WriteLine("Запуск збирача сміття (GC)...");
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Console.WriteLine("\nРоботу програми завершено.");
        }

        // Окремий метод, щоб об'єкт точно втратив посилання
        private static void CreateAndAbandonTracker()
        {
            var tracker3 = new GPSTracker("DEV-103");
            Console.WriteLine(tracker3.GetCurrentLocation());
            // Dispose() НЕ викликається, посилання втрачається після виходу з методу
        }
    }
}