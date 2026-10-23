namespace IndependentWork4
{
    /// <summary>
    /// Крок 5: розширення ієрархії. Успадковується від Book (а не від LibraryItem),
    /// бо аудіокнига - це книга, у якої є ще й оповідач.
    /// </summary>
    public class AudioBook : Book
    {
        private string _narrator;

        public string Narrator
        {
            get { return _narrator; }
            set { _narrator = value; }
        }

        public AudioBook(string uniqueId, string title, int publicationYear, string author, string narrator)
            : base(uniqueId, title, publicationYear, author)
        {
            _narrator = narrator;
        }

        public override void DisplayDetails()
        {
            base.DisplayDetails(); // виведе базову інформацію + автора
            Console.WriteLine($"    Формат: аудіокнига, читає: {Narrator}");
        }
    }
}
