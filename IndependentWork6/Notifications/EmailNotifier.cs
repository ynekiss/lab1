namespace IndependentWork6.Notifications
{
    public class EmailNotifier : INotifier
    {
        private readonly string _email;

        public EmailNotifier(string email)
        {
            _email = email;
        }

        public string ChannelName => "Email";

        public void Send(string message)
        {
            // Унікальна логіка: тема + тіло листа
            Console.WriteLine($"[Email] To: {_email}");
            Console.WriteLine($"        Subject: Сповіщення");
            Console.WriteLine($"        Body: {message}");
        }
    }
}
