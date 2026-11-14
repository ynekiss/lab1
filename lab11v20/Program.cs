using System.Text;

namespace lab11v20;

// ===================== Сутності (варіант 20: блоги, пости, теги) =====================

public class Tag
{
    public int Id { get; }
    public string Name { get; }

    public Tag(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString() => $"#{Name}";
}

public class Post
{
    public int Id { get; }
    public string Title { get; }
    public List<Tag> Tags { get; }

    public Post(int id, string title, List<Tag> tags)
    {
        Id = id;
        Title = title;
        Tags = tags;
    }

    public override string ToString() => $"[{Id}] {Title}";
}

public class Blog
{
    public int Id { get; }
    public string Name { get; }
    public List<Post> Posts { get; }

    public Blog(int id, string name, List<Post> posts)
    {
        Id = id;
        Name = name;
        Posts = posts;
    }
}

// ===================== Методи розширення =====================

public static class BlogExtensions
{
    /// <summary>
    /// Метод за варіантом: чи має пост тег із заданою назвою (без урахування регістру та символу #).
    /// </summary>
    public static bool HasTag(this Post post, string tagName)
    {
        ArgumentNullException.ThrowIfNull(post);
        if (string.IsNullOrWhiteSpace(tagName))
            return false;

        string normalized = tagName.Trim().TrimStart('#');
        return post.Tags.Any(t => string.Equals(t.Name, normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Узагальнена пагінація для будь-якої послідовності: номер сторінки починається з 1.
    /// </summary>
    public static IEnumerable<T> Page<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Номер сторінки починається з 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Розмір сторінки має бути більшим за 0.");

        return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}

// ===================== Тестові дані =====================

public static class TestData
{
    public static List<Blog> CreateBlogs()
    {
        var csharp = new Tag(1, "csharp");
        var dotnet = new Tag(2, "dotnet");
        var linq = new Tag(3, "linq");
        var oop = new Tag(4, "oop");
        var patterns = new Tag(5, "patterns");
        var web = new Tag(6, "web");
        var javascript = new Tag(7, "javascript");
        var css = new Tag(8, "css");
        var databases = new Tag(9, "databases");
        var sql = new Tag(10, "sql");
        var career = new Tag(11, "career");
        var testing = new Tag(12, "testing");

        return new List<Blog>
        {
            new(1, "C# щодня", new List<Post>
            {
                new(1, "Вступ до LINQ", new() { csharp, linq, dotnet }),
                new(2, "SelectMany на прикладах", new() { csharp, linq }),
                new(3, "Методи розширення", new() { csharp, dotnet }),
                new(4, "Інкапсуляція і властивості", new() { csharp, oop }),
                new(5, "Патерн Стратегія", new() { oop, patterns }),
                new(6, "Юніт-тести з xUnit", new() { csharp, testing, dotnet }),
                new(7, "Records у C#", new() { csharp, dotnet })
            }),
            new(2, "Front-end нотатки", new List<Post>
            {
                new(8, "Flexbox за 10 хвилин", new() { css, web }),
                new(9, "Асинхронність у JavaScript", new() { javascript, web }),
                new(10, "Тестування компонентів", new() { javascript, testing }),
                new(11, "CSS Grid", new() { css, web }),
                new(12, "Перший застосунок на Blazor", new() { csharp, web, dotnet })
            }),
            new(3, "Бази даних простими словами", new List<Post>
            {
                new(13, "JOIN без болю", new() { sql, databases }),
                new(14, "Індекси і продуктивність", new() { sql, databases }),
                new(15, "EF Core: перші кроки", new() { csharp, databases, dotnet }),
                new(16, "LINQ to Entities", new() { linq, databases, csharp })
            }),
            new(4, "Шлях розробника", new List<Post>
            {
                new(17, "Як підготуватися до співбесіди", new() { career }),
                new(18, "SOLID на співбесіді", new() { oop, patterns, career }),
                new(19, "Пет-проєкт у портфоліо", new() { career, web }),
                new(20, "Навіщо джуну тести", new() { testing, career })
            })
        };
    }
}

// ===================== Програма =====================

public class Program
{
    private const int PageSize = 5;

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        List<Blog> blogs = TestData.CreateBlogs();
        Console.WriteLine($"Блогів: {blogs.Count}, постів: {blogs.Sum(b => b.Posts.Count)}");
        Console.WriteLine();

        // ---------- 1. SelectMany: усі унікальні теги з усіх постів усіх блогів ----------
        Console.WriteLine("=== 1. SelectMany: унікальні теги з усіх постів усіх блогів ===");

        // Select дає колекцію колекцій: для кожного блогу — список постів
        IEnumerable<List<Post>> nested = blogs.Select(b => b.Posts);
        Console.WriteLine($"Select(b => b.Posts): {nested.Count()} вкладені списки");

        // SelectMany "розгортає" їх в одну плоску послідовність
        List<Tag> allTags = blogs
            .SelectMany(b => b.Posts)
            .SelectMany(p => p.Tags)
            .ToList();
        Console.WriteLine($"SelectMany(...).SelectMany(p => p.Tags): {allTags.Count} тегів разом із повторами");

        List<Tag> uniqueTags = allTags
            .DistinctBy(t => t.Id)
            .OrderBy(t => t.Name)
            .ToList();
        Console.WriteLine($"Унікальних тегів: {uniqueTags.Count}");
        Console.WriteLine(string.Join(", ", uniqueTags));
        Console.WriteLine();

        // SelectMany з resultSelector: зберігаємо зв'язок "блог - пост"
        Console.WriteLine("SelectMany з resultSelector — скільки разів тег використано (топ-5):");
        var tagUsage = blogs
            .SelectMany(b => b.Posts, (blog, post) => new { Blog = blog.Name, Post = post })
            .SelectMany(x => x.Post.Tags, (x, tag) => new { x.Blog, Tag = tag.Name })
            .GroupBy(x => x.Tag)
            .Select(g => new { Tag = g.Key, Count = g.Count(), Blogs = g.Select(x => x.Blog).Distinct().Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Tag)
            .Take(5);
        foreach (var t in tagUsage)
            Console.WriteLine($"  #{t.Tag,-10} постів: {t.Count,2}, блогів: {t.Blogs}");
        Console.WriteLine();

        // ---------- 2. Пагінація: усі пости по 5 на сторінку ----------
        Console.WriteLine($"=== 2. Пагінація: усі пости по {PageSize} на сторінку ===");
        var allPosts = blogs
            .SelectMany(b => b.Posts, (blog, post) => new { Blog = blog.Name, Post = post })
            .OrderBy(x => x.Post.Id)
            .ToList();

        int totalPages = (int)Math.Ceiling(allPosts.Count / (double)PageSize);
        for (int page = 1; page <= totalPages; page++)
        {
            // Skip/Take: пропускаємо попередні сторінки і беремо поточну
            var pageItems = allPosts.Skip((page - 1) * PageSize).Take(PageSize);
            Console.WriteLine($"--- Сторінка {page} з {totalPages} ---");
            foreach (var item in pageItems)
                Console.WriteLine($"  {item.Post,-38} ({item.Blog})");
        }

        // Те саме через власний метод розширення Page<T>
        Console.WriteLine();
        Console.WriteLine("Метод розширення Page(3, 5) — третя сторінка:");
        foreach (var item in allPosts.Page(3, PageSize))
            Console.WriteLine($"  {item.Post}");
        Console.WriteLine($"Сторінка 6 (за межами даних): {allPosts.Page(6, PageSize).Count()} постів");
        Console.WriteLine();

        // ---------- 3. Custom Extension: HasTag(string tagName) для Post ----------
        Console.WriteLine("=== 3. Метод розширення HasTag(string tagName) для Post ===");
        Post post = allPosts[0].Post;
        Console.WriteLine($"Пост: {post}, теги: {string.Join(", ", post.Tags)}");
        Console.WriteLine($"  post.HasTag(\"linq\")    = {post.HasTag("linq")}");
        Console.WriteLine($"  post.HasTag(\"LINQ\")    = {post.HasTag("LINQ")}");
        Console.WriteLine($"  post.HasTag(\"#dotnet\") = {post.HasTag("#dotnet")}");
        Console.WriteLine($"  post.HasTag(\"css\")     = {post.HasTag("css")}");
        Console.WriteLine();

        // Використання в LINQ-запиті
        string searchTag = "testing";
        Console.WriteLine($"Пости з тегом #{searchTag} (Where + HasTag):");
        foreach (var item in allPosts.Where(x => x.Post.HasTag(searchTag)))
            Console.WriteLine($"  {item.Post} ({item.Blog})");

        Console.WriteLine();
        Console.WriteLine("Пости з тегами #csharp і #linq одночасно:");
        foreach (var item in allPosts.Where(x => x.Post.HasTag("csharp") && x.Post.HasTag("linq")))
            Console.WriteLine($"  {item.Post}");
    }
}

/*
 * Контрольні питання
 *
 * 1. SelectMany проєктує кожен елемент у колекцію і об'єднує всі отримані колекції в одну плоску
 *    послідовність. Select для blogs.Select(b => b.Posts) повертає IEnumerable<List<Post>> (4 списки),
 *    а blogs.SelectMany(b => b.Posts) — IEnumerable<Post> (20 постів підряд).
 * 2. Пагінація: source.Skip((page - 1) * pageSize).Take(pageSize). Кількість сторінок —
 *    Math.Ceiling(count / (double)pageSize). Перед пагінацією дані варто впорядкувати (OrderBy),
 *    щоб сторінки були стабільними.
 * 3. Метод розширення — статичний метод статичного класу з модифікатором this перед першим
 *    параметром. Викликається як метод екземпляра. Усі оператори LINQ (Where, Select, ...) —
 *    це методи розширення для IEnumerable<T>, тому власні розширення природно вбудовуються в ланцюжок.
 * 4. Page<T>(this IEnumerable<T> source, int pageNumber, int pageSize) — узагальнена пагінація для
 *    будь-якої послідовності: ховає формулу Skip/Take і перевіряє аргументи.
 */
