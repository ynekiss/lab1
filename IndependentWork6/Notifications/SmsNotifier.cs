namespace IndependentWork6.Notifications
{
    public class SmsNotifier : INotifier
    {
        private const int MaxLength = 70; // ліміт одного SMS кирилицею
        private readonly string _phone;

        public SmsNotifier(string phone)
        {
            _phone = phone;
        }

        public string ChannelName => "SMS";

        public void Send(string message)
        {
            // Унікальна логіка: обрізання тексту під ліміт SMS
            string text = message.Length > MaxLength
                ? message.Substring(0, MaxLength - 3) + "..."
                : message;
            Console.WriteLine($"[SMS]   To: {_phone} ({text.Length} симв.): {text}");
        }
    }
}
