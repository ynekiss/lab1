namespace IndependentWork6.FileProcessing
{
    /// <summary>
    /// Сценарій 2: усі обробники мають СПІЛЬНІ кроки (відкрити, закрити, вивести підсумок)
    /// і різну логіку обробки вмісту. Тому обрано абстрактний клас із
    /// шаблонним методом (Template Method).
    /// </summary>
    public abstract class FileProcessor
    {
        // Спільний стан, який інтерфейс зберігати не може
        protected string FilePath { get; private set; }
        protected string Content { get; private set; }
        private int _processedCount;

        public abstract string FormatName { get; }

        /// <summary>
        /// Шаблонний метод: фіксований порядок кроків. Не віртуальний -
        /// нащадки не можуть порушити послідовність open -> process -> close.
        /// </summary>
        public void Process(string path)
        {
            Open(path);
            try
            {
                string result = ProcessContent(Content);
                _processedCount++;
                Console.WriteLine($"    Результат: {result}");
            }
            finally
            {
                Close();
            }
        }

        public int ProcessedCount => _processedCount;

        // Спільний крок 1 - реалізація одна для всіх
        private void Open(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Файл не знайдено", path);
            }
            FilePath = path;
            Content = File.ReadAllText(path);
            Console.WriteLine($"[{FormatName}] Відкрито {Path.GetFileName(path)} ({Content.Length} символів)");
        }

        // Спільний крок 2
        private void Close()
        {
            Console.WriteLine($"[{FormatName}] Закрито {Path.GetFileName(FilePath)}");
            Content = null;
            FilePath = null;
        }

        // Унікальна частина - кожен нащадок реалізує по-своєму
        protected abstract string ProcessContent(string content);
    }
}
