namespace IndependentWork8
{
    public class FileMetadata
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string Hash { get; set; }

        public override string ToString() =>
            $"{FileName} ({Size:N0} B, змінено {LastModified:dd.MM.yyyy HH:mm}, hash {Hash})";
    }

    /// <summary>
    /// Сценарій 1: кеш метаданих файлів на основі Dictionary.
    /// Ключ - шлях до файлу. Порівняння без урахування регістру,
    /// бо у Windows "Report.TXT" і "report.txt" - той самий файл.
    /// </summary>
    public class FileMetadataCache
    {
        private readonly Dictionary<string, FileMetadata> _cache =
            new Dictionary<string, FileMetadata>(StringComparer.OrdinalIgnoreCase);

        public int Count => _cache.Count;

        /// <summary>Додає або оновлює запис. O(1) в середньому.</summary>
        public void Add(string path, FileMetadata metadata)
        {
            _cache[path] = metadata;
        }

        /// <summary>Повертає метадані або null, якщо файл не кешовано. O(1).</summary>
        public FileMetadata Get(string path)
        {
            return _cache.TryGetValue(path, out FileMetadata metadata) ? metadata : null;
        }

        /// <summary>O(1).</summary>
        public bool Remove(string path)
        {
            return _cache.Remove(path);
        }

        /// <summary>O(1).</summary>
        public bool Contains(string path)
        {
            return _cache.ContainsKey(path);
        }

        /// <summary>Перевіряє, чи змінився файл з моменту кешування.</summary>
        public bool IsChanged(string path, DateTime lastModified)
        {
            FileMetadata cached = Get(path);
            return cached == null || cached.LastModified != lastModified;
        }
    }
}
