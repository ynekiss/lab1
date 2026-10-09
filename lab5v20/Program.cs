using System;
using System.Collections.Generic;

namespace lab5v20
{
    public class Command
    {
        public string Name { get; set; }

        public Command(string name)
        {
            Name = name;
        }

        public virtual void Execute()
        {
            Console.WriteLine($"[Command] Виконання базової команди: {Name}");
        }
    }

    public class OpenFileCommand : Command
    {
        public string FilePath { get; set; }

        public OpenFileCommand(string name, string filePath) : base(name)
        {
            FilePath = filePath;
        }

        public override void Execute()
        {
            Console.WriteLine($"[OpenFileCommand] Відкриття файлу за шляхом: {FilePath}");
        }
    }

    public class SaveFileCommand : Command
    {
        public string Content { get; set; }

        public SaveFileCommand(string name, string content) : base(name)
        {
            Content = content;
        }

        public override void Execute()
        {
            Console.WriteLine($"[SaveFileCommand] Збереження вмісту: \"{Content}\"");
        }
    }

    public class CloseFileCommand : Command
    {
        public bool ConfirmSave { get; set; }

        public CloseFileCommand(string name, bool confirmSave) : base(name)
        {
            ConfirmSave = confirmSave;
        }

        public override void Execute()
        {
            string saveStatus = ConfirmSave ? "з попереднім збереженням" : "без збереження";
            Console.WriteLine($"[CloseFileCommand] Закриття файлу ({saveStatus}).");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== Лабораторна робота №5 (Варіант 20) ===");
            Console.WriteLine("Студент: Rybonka\n");

            List<Command> commandHistory = new List<Command>
            {
                new OpenFileCommand("Відкрити документ", "C:/Documents/Report.docx"),
                new SaveFileCommand("Зберегти документ", "Оновлений текст звіту з ООП"),
                new SaveFileCommand("Автозбереження", "Резервна копія матеріалів"),
                new CloseFileCommand("Закрити документ", true)
            };

            Console.WriteLine("=== Виконання команд у циклі (Поліморфізм) ===");
            
            List<string> executedLogs = new List<string>();
            int executedCount = 0;

            foreach (Command cmd in commandHistory)
            {
                cmd.Execute();

                executedCount++;
                executedLogs.Add($"[#{executedCount}] Команда '{cmd.Name}' успішно виконана.");
            }

            Console.WriteLine("\n=== Агрегація результатів ===");
            Console.WriteLine($"Всього виконано команд: {executedCount}");
            Console.WriteLine("Список зафіксованих дій:");
            foreach (string log in executedLogs)
            {
                Console.WriteLine($"  - {log}");
            }
        }
    }
}