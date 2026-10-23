namespace IndependentWork6.Notifications
{
    /// <summary>
    /// Push-сповіщувач додатково реалізує ILogger -
    /// клас може мати кілька "ролей", що можливо тільки з інтерфейсами.
    /// </summary>
    public class PushNotifier : INotifier, ILogger
    {
        private readonly string _deviceToken;
        private readonly List<string> _log = new List<string>();

        public PushNotifier(string deviceToken)
        {
            _deviceToken = deviceToken;
        }

        public string ChannelName => "Push";

        public void Send(string message)
        {
            Console.WriteLine($"[Push]  Device: {_deviceToken} | {{\"title\":\"Нове сповіщення\",\"body\":\"{message}\"}}");
            Log($"push sent to {_deviceToken}");
        }

        public void Log(string entry)
        {
            _log.Add($"{DateTime.Now:HH:mm:ss} {entry}");
        }

        public IReadOnlyList<string> Entries => _log;
    }

    public interface ILogger
    {
        void Log(string entry);
        IReadOnlyList<string> Entries { get; }
    }
}
