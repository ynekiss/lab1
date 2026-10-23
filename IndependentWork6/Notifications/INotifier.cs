namespace IndependentWork6.Notifications
{
    /// <summary>
    /// Сценарій 1: контракт "вмію надсилати повідомлення".
    /// Канали між собою не пов'язані, спільного коду в них немає,
    /// тому обрано інтерфейс, а не абстрактний клас.
    /// </summary>
    public interface INotifier
    {
        string ChannelName { get; }
        void Send(string message);
    }
}
