namespace IndependentWork1
{
    /// <summary>
    /// Авіарейс з обмеженою кількістю місць.
    /// </summary>
    public class Flight
    {
        // Приватні поля
        private string _number;
        private string _destination;
        private int _capacity;
        private int _bookedSeats;
        private decimal _ticketPrice;

        // Read-only властивість
        public string Number
        {
            get { return _number; }
        }

        // Публічна властивість з get і set
        public string Destination
        {
            get { return _destination; }
            set { _destination = value; }
        }

        public int FreeSeats
        {
            get { return _capacity - _bookedSeats; }
        }

        // Конструктор
        public Flight(string number, string destination, int capacity, decimal ticketPrice)
        {
            _number = number;
            _destination = destination;
            _capacity = capacity;
            _ticketPrice = ticketPrice;
            _bookedSeats = 0;
        }

        // Бронювання з перевіркою стану (чи вистачає місць)
        public bool BookSeats(int count)
        {
            if (count <= 0 || count > FreeSeats)
            {
                return false;
            }
            _bookedSeats += count;
            return true;
        }

        // Обчислення: заповненість рейсу у відсотках
        public double GetOccupancyPercent()
        {
            return (double)_bookedSeats / _capacity * 100;
        }

        // Обчислення: виручка від проданих квитків
        public decimal GetRevenue()
        {
            return _bookedSeats * _ticketPrice;
        }
    }
}
