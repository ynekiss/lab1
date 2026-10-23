using System.Text.RegularExpressions;

namespace InvertedIndexDemo
{
    public static class TextProcessor
    {
        // Слово = літери/цифри/_ плюс символи # і + (щоб "C#" і "C++" не перетворювались на "c")
        private static readonly Regex WordRegex = new Regex(@"[\w#+]+", RegexOptions.Compiled);

        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "a", "an", "the", "is", "are", "was", "were",
            "in", "on", "at", "to", "for", "of", "and", "or"
        };

        /// <summary>
        /// Розбиває текст на слова, видаляє пунктуацію, переводить у нижній регістр.
        /// "Hello, world!" -> ["hello", "world"]
        /// </summary>
        public static IEnumerable<string> Tokenize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            foreach (Match match in WordRegex.Matches(text.ToLowerInvariant()))
            {
                yield return match.Value;
            }
        }

        /// <summary>
        /// Видаляє стоп-слова (a, an, the, is, are, ...).
        /// </summary>
        public static IEnumerable<string> RemoveStopWords(IEnumerable<string> words)
        {
            return words.Where(w => !StopWords.Contains(w));
        }

        /// <summary>
        /// Нормалізація одного слова запиту тими самими правилами, що й Tokenize.
        /// </summary>
        public static string Normalize(string word)
        {
            return Tokenize(word).FirstOrDefault() ?? string.Empty;
        }
    }
}
