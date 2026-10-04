using System;

namespace lab3v20
{
    public class GPSTracker : IDisposable
    {
        private string _deviceId;
        private bool _isTracking;
        private bool _disposed = false;

        public GPSTracker(string deviceId)
        {
            _deviceId = deviceId;
            _isTracking = true;
            Console.WriteLine($"[GPSTracker] Пристрій {_deviceId} успішно ініціалізовано. Відстеження увімкнено.");
        }

        public string GetCurrentLocation()
        {
            if (!_isTracking || _disposed)
            {
                return $"[GPSTracker] Помилка: пристрій {_deviceId} вимкнений або відстеження зупинено.";
            }

            Random rnd = new Random();
            double latitude = 50.4501 + (rnd.NextDouble() - 0.5) * 0.1;
            double longitude = 30.5234 + (rnd.NextDouble() - 0.5) * 0.1;

            return $"[GPSTracker] Пристрій {_deviceId} -> Широта: {latitude:F4}, Довгота: {longitude:F4}";
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _isTracking = false;
                    Console.WriteLine($"[GPSTracker] Dispose: Зупиняємо відстеження для пристрою {_deviceId}.");
                }
                _disposed = true;
            }
        }

        ~GPSTracker()
        {
            Dispose(false);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== Лабораторна робота №3: GPSTracker ===");

            using (GPSTracker tracker = new GPSTracker("GPS-RYBONKA-20"))
            {
                Console.WriteLine(tracker.GetCurrentLocation());
                Console.WriteLine(tracker.GetCurrentLocation());
            } 

            Console.WriteLine("\nСпроба викликати метод після Dispose:");
            GPSTracker manualTracker = new GPSTracker("GPS-TEST-01");
            Console.WriteLine(manualTracker.GetCurrentLocation());
            
            manualTracker.Dispose();
            Console.WriteLine(manualTracker.GetCurrentLocation());
        }
    }
}