namespace IndependentWork8
{
    /// <summary>
    /// Сценарій 2: теги статей блогу.
    /// Для кожної статті - HashSet тегів (тег не може повторюватись).
    /// </summary>
    public class TagManager
    {
        // ArticleId -> Tags
        private readonly Dictionary<string, HashSet<string>> _articleTags =
            new Dictionary<string, HashSet<string>>();

        public void AddTag(string articleId, string tag)
        {
            if (!_articleTags.TryGetValue(articleId, out HashSet<string> tags))
            {
                // Теги без урахування регістру: "CSharp" і "csharp" - один тег
                tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _articleTags[articleId] = tags;
            }

            tags.Add(tag.Trim()); // дублікат просто не додасться
        }

        public void RemoveTag(string articleId, string tag)
        {
            if (_articleTags.TryGetValue(articleId, out HashSet<string> tags))
            {
                tags.Remove(tag);
            }
        }

        public HashSet<string> GetTags(string articleId)
        {
            return _articleTags.TryGetValue(articleId, out HashSet<string> tags)
                ? new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Усі унікальні теги всіх статей (UnionWith).</summary>
        public HashSet<string> GetAllUniqueTags()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HashSet<string> tags in _articleTags.Values)
            {
                result.UnionWith(tags);
            }
            return result;
        }

        /// <summary>Спільні теги двох статей (IntersectWith).</summary>
        public HashSet<string> FindCommonTags(string articleId1, string articleId2)
        {
            HashSet<string> result = GetTags(articleId1); // копія, оригінал не змінюємо
            result.IntersectWith(GetTags(articleId2));
            return result;
        }

        /// <summary>Теги, які є у першій статті, але відсутні в другій (ExceptWith).</summary>
        public HashSet<string> FindUniqueTags(string articleId, string otherArticleId)
        {
            HashSet<string> result = GetTags(articleId);
            result.ExceptWith(GetTags(otherArticleId));
            return result;
        }
    }
}
