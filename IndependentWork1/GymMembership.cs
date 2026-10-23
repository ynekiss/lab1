namespace IndependentWork1
{
    /// <summary>
    /// Абонемент у спортзал.
    /// </summary>
    public class GymMembership
    {
        // Приватні поля
        private string _ownerName;
        private DateTime _startDate;
        private int _durationDays;
        private int _visits;

        // Публічна властивість з get і set
        public string OwnerName
        {
            get { return _ownerName; }
            set { _ownerName = value; }
        }

        // Read-only властивість: кількість відвідувань змінюється тільки через метод Visit()
        public int Visits
        {
            get { return _visits; }
        }

        public DateTime EndDate
        {
            get { return _startDate.AddDays(_durationDays); }
        }

        // Конструктор
        public GymMembership(string ownerName, DateTime startDate, int durationDays)
        {
            _ownerName = ownerName;
            _startDate = startDate;
            _durationDays = durationDays;
            _visits = 0;
        }

        // Перевірка стану: чи діє абонемент на вказану дату
        public bool IsActive(DateTime date)
        {
            return date >= _startDate && date < EndDate;
        }

        // Відмітити відвідування (тільки якщо абонемент активний)
        public bool Visit(DateTime date)
        {
            if (!IsActive(date))
            {
                return false;
            }
            _visits++;
            return true;
        }

        // Обчислення: скільки днів залишилось
        public int DaysLeft(DateTime date)
        {
            if (!IsActive(date))
            {
                return 0;
            }
            return (EndDate - date.Date).Days;
        }
    }
}
