namespace IndependentWork4
{
    public class Book : LibraryItem
    {
        private string _author;

        public string Author
        {
            get { return _author; }
            set { _author = value; }
        }

        // Було:  public Book(string title, int publicationYear, string author)
        //            : base(title, publicationYear)
        // Після зміни базового конструктора довелося додати параметр uniqueId
        // і передати його в base(...), інакше проєкт не компілювався (помилка CS7036).
        public Book(string uniqueId, string title, int publicationYear, string author)
            : base(uniqueId, title, publicationYear)
        {
            _author = author;
        }

        public override void DisplayDetails()
        {
            base.DisplayDetails();
            Console.WriteLine($"    Тип: книга, автор: {Author}");
        }
    }
}
