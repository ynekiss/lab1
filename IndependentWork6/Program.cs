using System.Text;
using IndependentWork6.FileProcessing;
using IndependentWork6.Notifications;

namespace IndependentWork6
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            NotificationsDemo();
            Console.WriteLine();
            FileProcessingDemo();
        }

        private static void NotificationsDemo()
        {
            Console.WriteLine("=== Сценарій 1. Система сповіщень (інтерфейс INotifier) ===");

            PushNotifier push = new PushNotifier("device-7f3a");
            List<INotifier> notifiers = new List<INotifier>
            {
                new EmailNotifier("student@rfkit.edu.ua"),
                new SmsNotifier("+380671234567"),
                push
            };

            string message = "Самостійну роботу №6 перевірено. Оцінка доступна в електронному журналі коледжу.";
            foreach (INotifier notifier in notifiers)
            {
                notifier.Send(message); // поліморфний виклик
            }

            Console.WriteLine($"Канали: {string.Join(", ", notifiers.Select(n => n.ChannelName))}");

            // Той самий об'єкт можна використовувати і як ILogger
            ILogger logger = push;
            Console.WriteLine($"Записів у лозі Push: {logger.Entries.Count}");
        }

        private static void FileProcessingDemo()
        {
            Console.WriteLine("=== Сценарій 2. Обробка файлів (абстрактний клас FileProcessor) ===");

            string dir = CreateSampleFiles();

            // Колекція базового типу - абстрактного класу
            List<FileProcessor> processors = new List<FileProcessor>
            {
                new TextFileProcessor(),
                new XmlFileProcessor(),
                new CsvFileProcessor()
            };

            Dictionary<string, string> files = new Dictionary<string, string>
            {
                ["TXT"] = Path.Combine(dir, "notes.txt"),
                ["XML"] = Path.Combine(dir, "group.xml"),
                ["CSV"] = Path.Combine(dir, "grades.csv")
            };

            foreach (FileProcessor processor in processors)
            {
                processor.Process(files[processor.FormatName]); // спільні кроки + унікальна обробка
            }

            Console.WriteLine($"Оброблено файлів: {processors.Sum(p => p.ProcessedCount)}");
        }

        /// <summary>
        /// Створює тестові файли в каталозі проєкту, щоб розв'язок був самодостатнім.
        /// </summary>
        private static string CreateSampleFiles()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "samples");
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "notes.txt"),
                "Інтерфейс описує що вміє об'єкт.\nАбстрактний клас описує ким є об'єкт.\nОбидва дають поліморфізм.\n");

            File.WriteAllText(Path.Combine(dir, "group.xml"),
                "<group name=\"ІПЗ 3/2\">\n" +
                "  <student name=\"Іван\" />\n" +
                "  <student name=\"Марія\" />\n" +
                "  <student name=\"Олег\" />\n" +
                "</group>\n");

            File.WriteAllText(Path.Combine(dir, "grades.csv"),
                "name,subject,grade\nІван,ООП,5\nМарія,ООП,4\nОлег,ООП,5\nАнна,ООП,3\n");

            return dir;
        }
    }
}
