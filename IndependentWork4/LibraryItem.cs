namespace IndependentWork4
{
    /// <summary>
    /// Базовий клас бібліотечного елемента.
    /// Крок 4: додано поле _uniqueId, read-only властивість UniqueId
    /// і новий параметр uniqueId у конструкторі.
    /// </summary>
    public class LibraryItem
    {
        private string _title;
        private int _publicationYear;
        private readonly string _uniqueId; // НОВЕ (крок 4)

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public int PublicationYear
        {
            get { return _publicationYear; }
            set { _publicationYear = value; }
        }

        // НОВЕ (крок 4): тільки для читання
        public string UniqueId
        {
            get { return _uniqueId; }
        }

        // Було: public LibraryItem(string title, int publicationYear)
        public LibraryItem(string uniqueId, string title, int publicationYear)
        {
            _uniqueId = uniqueId;
            _title = title;
            _publicationYear = publicationYear;
        }

        public virtual void DisplayDetails()
        {
            Console.WriteLine($"[{UniqueId}] \"{Title}\", рік видання: {PublicationYear}");
        }
    }
}
