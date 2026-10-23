namespace IndependentWork1
{
    /// <summary>
    /// Розумний термостат у квартирі.
    /// </summary>
    public class Thermostat
    {
        // Приватні поля
        private string _room;
        private double _currentTemperature;
        private double _targetTemperature;

        // Властивість тільки для читання: кімнату після встановлення змінити не можна
        public string Room
        {
            get { return _room; }
        }

        // Публічна властивість з get і set (з перевіркою допустимого діапазону)
        public double TargetTemperature
        {
            get { return _targetTemperature; }
            set
            {
                if (value < 5 || value > 30)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Температура має бути від 5 до 30 °C.");
                }
                _targetTemperature = value;
            }
        }

        public double CurrentTemperature
        {
            get { return _currentTemperature; }
        }

        // Конструктор
        public Thermostat(string room, double currentTemperature, double targetTemperature)
        {
            _room = room;
            _currentTemperature = currentTemperature;
            TargetTemperature = targetTemperature;
        }

        // Метод перевірки стану: що зараз робить термостат
        public string GetMode()
        {
            double difference = _targetTemperature - _currentTemperature;

            if (difference > 0.5)
            {
                return "Обігрів";
            }
            if (difference < -0.5)
            {
                return "Охолодження";
            }
            return "Очікування (температура в нормі)";
        }

        // Метод з обчисленням: імітуємо роботу протягом кількох хвилин
        // (термостат змінює температуру на 0.1 °C за хвилину)
        public void Simulate(int minutes)
        {
            for (int i = 0; i < minutes; i++)
            {
                if (_currentTemperature < _targetTemperature - 0.05)
                {
                    _currentTemperature += 0.1;
                }
                else if (_currentTemperature > _targetTemperature + 0.05)
                {
                    _currentTemperature -= 0.1;
                }
            }
            _currentTemperature = Math.Round(_currentTemperature, 1);
        }

        public void PrintInfo()
        {
            Console.WriteLine($"Термостат [{_room}]: зараз {_currentTemperature:F1} °C, ціль {_targetTemperature:F1} °C, режим: {GetMode()}");
        }
    }
}
