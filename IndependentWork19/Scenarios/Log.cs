using System.Diagnostics;

namespace IndependentWork19.Scenarios;

/// <summary>Простий консольний логер з відносним часом від початку сценарію.</summary>
public static class Log
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static readonly object Sync = new();

    public static void Restart() => Clock.Restart();

    public static void Info(string message) => Write(message, ConsoleColor.Gray);
    public static void Success(string message) => Write(message, ConsoleColor.Green);
    public static void Warn(string message) => Write(message, ConsoleColor.Yellow);
    public static void Error(string message) => Write(message, ConsoleColor.Red);
    public static void Polly(string message) => Write(message, ConsoleColor.Cyan);

    private static void Write(string message, ConsoleColor color)
    {
        lock (Sync) // колбеки Polly можуть викликатися з інших потоків
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"  [{Clock.Elapsed.TotalSeconds,6:F2} с] {message}");
            Console.ForegroundColor = old;
        }
    }

    public static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 90));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 90));
    }
}
