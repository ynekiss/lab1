using System;
using System.Collections.Generic;
using System.Linq;

namespace lab7v20
{
    public interface IEntity
    {
        int Id { get; set; }
    }

    public class Event : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
    }

    public interface IRepository<T> where T : class, IEntity
    {
        T? GetById(int id);
        IEnumerable<T> GetAll();
        void Add(T item);
        void Update(T item);
        void Delete(int id);
    }

    public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly List<T> _items = new();

        public T? GetById(int id)
        {
            return _items.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<T> GetAll()
        {
            return _items.ToList();
        }

        public void Add(T item)
        {
            if (item == null) 
                throw new ArgumentNullException(nameof(item));
                
            if (_items.Any(x => x.Id == item.Id))
                throw new InvalidOperationException($"Елемент з ID {item.Id} вже існує.");

            _items.Add(item);
        }

        public void Update(T item)
        {
            if (item == null) 
                throw new ArgumentNullException(nameof(item));

            var existing = GetById(item.Id);
            if (existing == null)
                throw new KeyNotFoundException($"Елемент з ID {item.Id} не знайдено.");

            int index = _items.IndexOf(existing);
            _items[index] = item;
        }

        public void Delete(int id)
        {
            var item = GetById(id);
            if (item == null)
                throw new KeyNotFoundException($"Елемент з ID {id} не знайдено.");

            _items.Remove(item);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var repository = new InMemoryRepository<Event>();

            var ev1 = new Event { Id = 1, Name = "IT Conference", Location = "Kyiv", EventDate = new DateTime(2026, 11, 15) };
            var ev2 = new Event { Id = 2, Name = "DevOps Workshop", Location = "Lviv", EventDate = new DateTime(2026, 12, 01) };
            
            repository.Add(ev1);
            repository.Add(ev2);

            Console.WriteLine("=== Усі події ===");
            foreach (var item in repository.GetAll())
            {
                Console.WriteLine($"ID: {item.Id} | {item.Name} | {item.Location} | {item.EventDate:yyyy-MM-dd}");
            }

            ev1.Location = "Kyiv (Online)";
            repository.Update(ev1);

            Console.WriteLine("\n=== Після оновлення ID 1 ===");
            var updated = repository.GetById(1);
            Console.WriteLine($"ID: {updated?.Id} | {updated?.Name} | {updated?.Location}");

            repository.Delete(2);
            Console.WriteLine($"\nКількість елементів після видалення ID 2: {repository.GetAll().Count()}");
        }
    }
}