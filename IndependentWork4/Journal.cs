namespace IndependentWork4
{
    public class Journal : LibraryItem
    {
        private int _issueNumber;

        public int IssueNumber
        {
            get { return _issueNumber; }
            set { _issueNumber = value; }
        }

        // Було:  public Journal(string title, int publicationYear, int issueNumber)
        //            : base(title, publicationYear)
        // Аналогічно до Book: додано uniqueId і передано в base(...).
        public Journal(string uniqueId, string title, int publicationYear, int issueNumber)
            : base(uniqueId, title, publicationYear)
        {
            _issueNumber = issueNumber;
        }

        public override void DisplayDetails()
        {
            base.DisplayDetails();
            Console.WriteLine($"    Тип: журнал, випуск №{IssueNumber}");
        }
    }
}
