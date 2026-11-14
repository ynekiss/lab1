using System.Text;

namespace lab13v20;

// ===================== Власний виняток (варіант 20) =====================

/// <summary>
/// Книгу неможливо видати: її немає в каталозі або всі примірники вже на руках.
/// </summary>
public class BookNotAvailableException : Exception
{
    public string BookTitle { get; } = string.Empty;

    /// <summary>Хто намагався взяти книгу.</summary>
    public string? UserName { get; }

    /// <summary>Чи існує книга в каталозі (false — книги немає взагалі, true — усі примірники видано).</summary>
    public bool ExistsInCatalog { get; }

    // --- стандартні конструктори ---

    public BookNotAvailableException()
        : base("Книга недоступна для видачі.")
    {
    }

    public BookNotAvailableException(string message)
        : base(message)
    {
    }

    public BookNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    // --- конструктор з контекстом помилки ---

    public BookNotAvailableException(string bookTitle, string? userName, bool existsInCatalog)
        : base(existsInCatalog
            ? $"Книга «{bookTitle}» зараз недоступна: усі примірники видано."
            : $"Книги «{bookTitle}» немає в каталозі бібліотеки.")
    {
        BookTitle = bookTitle;
        UserName = userName;
        ExistsInCatalog = existsInCatalog;
    }
}

// ===================== Result<T> =====================

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(T? value, bool isSuccess, string? error)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value, true, null);

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Текст помилки не може бути порожнім.", nameof(error));
        return new(default, false, error);
    }

    public static Result<T> Failure(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return new(default, false, $"{ex.GetType().Name}: {ex.Message}");
    }

    public override string ToString() => IsSuccess ? $"Success({Value})" : $"Failure(\"{Error}\")";
}

// ===================== Модель і сервіс =====================

public class Book
{
    public string Title { get; }
    public string Author { get; }
    public int TotalCopies { get; }
    public int AvailableCopies { get; set; }

    public Book(string title, string author, int copies)
    {
        Title = title;
        Author = author;
        TotalCopies = copies;
        AvailableCopies = copies;
    }
}

public class LibraryService
{
    private readonly Dictionary<string, Book> _books = new(StringComparer.OrdinalIgnoreCase);

    // хто які книги взяв: користувач -> назви книг
    private readonly Dictionary<string, List<string>> _loans = new(StringComparer.OrdinalIgnoreCase);

    public void AddBook(string title, string author, int copies) => _books[title] = new Book(title, author, copies);

    /// <summary>
    /// Видача книги. Якщо видати неможливо — це порушення контракту методу, тому викидаємо
    /// BookNotAvailableException (викликач мусить це обробити, а не "забути").
    /// </summary>
    public void BorrowBook(string bookTitle, string userName)
    {
        if (string.IsNullOrWhiteSpace(bookTitle))
            throw new ArgumentException("Назва книги не може бути порожньою.", nameof(bookTitle));
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("Ім'я читача не може бути порожнім.", nameof(userName));

        if (!_books.TryGetValue(bookTitle, out Book? book))
            throw new BookNotAvailableException(bookTitle, userName, existsInCatalog: false);

        if (book.AvailableCopies == 0)
            throw new BookNotAvailableException(book.Title, userName, existsInCatalog: true);

        book.AvailableCopies--;
        if (!_loans.TryGetValue(userName, out List<string>? userBooks))
        {
            userBooks = new List<string>();
            _loans[userName] = userBooks;
        }
        userBooks.Add(book.Title);
    }

    /// <summary>
    /// Повернення книги. Спроба повернути книгу, яку читач не брав, — очікувана ситуація
    /// (помилка введення на стійці видачі), тому повертаємо Result замість винятку.
    /// </summary>
    public Result<bool> ReturnBook(string bookTitle, string userName)
    {
        if (string.IsNullOrWhiteSpace(bookTitle) || string.IsNullOrWhiteSpace(userName))
            return Result<bool>.Failure("Потрібно вказати назву книги та ім'я читача.");

        if (!_loans.TryGetValue(userName, out List<string>? userBooks))
            return Result<bool>.Failure($"Читач {userName} не має виданих книг.");

        string? loan = userBooks.FirstOrDefault(t => string.Equals(t, bookTitle, StringComparison.OrdinalIgnoreCase));
        if (loan is null)
            return Result<bool>.Failure($"Книгу «{bookTitle}» читач {userName} не брав.");

        userBooks.Remove(loan);
        _books[loan].AvailableCopies++;
        return Result<bool>.Success(true);
    }

    /// <summary>Пошук книги: відсутність результату — нормальний випадок, тому Result.</summary>
    public Result<Book> FindBook(string bookTitle)
    {
        try
        {
            return _books.TryGetValue(bookTitle, out Book? book)
                ? Result<Book>.Success(book)
                : Result<Book>.Failure($"Книгу «{bookTitle}» не знайдено.");
        }
        catch (ArgumentNullException ex)
        {
            // Failure(Exception): технічну помилку перетворюємо на Result
            return Result<Book>.Failure(ex);
        }
    }

    public IReadOnlyList<string> GetUserBooks(string userName) =>
        _loans.TryGetValue(userName, out List<string>? books) ? books : Array.Empty<string>();
}

// ===================== Програма =====================

public class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var library = new LibraryService();
        library.AddBook("Кобзар", "Тарас Шевченко", 2);
        library.AddBook("Тигролови", "Іван Багряний", 1);
        library.AddBook("Clean Code", "Robert C. Martin", 1);

        // ---------- 1. BorrowBook: власний виняток + try-catch ----------
        Console.WriteLine("=== 1. BorrowBook: обробка BookNotAvailableException через try-catch ===");
        var requests = new (string Title, string User)[]
        {
            ("Тигролови", "Олена"),
            ("Кобзар", "Андрій"),
            ("Тигролови", "Максим"),       // єдиний примірник уже видано
            ("Майстер і Маргарита", "Ірина"), // немає в каталозі
            ("", "Ірина")                   // некоректні дані
        };

        foreach (var (title, user) in requests)
        {
            try
            {
                library.BorrowBook(title, user);
                Console.WriteLine($"  ✔ {user} взяв(ла) «{title}»");
            }
            catch (BookNotAvailableException ex)
            {
                Console.WriteLine($"  ✘ {ex.Message}");
                Console.WriteLine($"     BookTitle = «{ex.BookTitle}», UserName = {ex.UserName}, ExistsInCatalog = {ex.ExistsInCatalog}");
                Console.WriteLine(ex.ExistsInCatalog
                    ? "     → Пропонуємо читачу стати в чергу на книгу."
                    : "     → Пропонуємо читачу замовити книгу через міжбібліотечний абонемент.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"  ✘ Некоректний запит: {ex.Message}");
            }
        }
        Console.WriteLine();

        // ---------- 2. ReturnBook: Result<bool> + перевірка IsSuccess/Error ----------
        Console.WriteLine("=== 2. ReturnBook: обробка Result<bool> через IsSuccess / Error ===");
        var returns = new (string Title, string User)[]
        {
            ("Тигролови", "Максим"),   // Максим цю книгу не брав
            ("Clean Code", "Олена"),   // Олена брала іншу книгу
            ("Кобзар", "Ірина"),       // Ірина нічого не брала
            ("Тигролови", "Олена")     // коректне повернення
        };

        foreach (var (title, user) in returns)
        {
            Result<bool> result = library.ReturnBook(title, user);
            if (result.IsSuccess)
                Console.WriteLine($"  ✔ {user} повернув(ла) «{title}». {result}");
            else
                Console.WriteLine($"  ✘ Повернення не прийнято: {result.Error}");
        }
        Console.WriteLine();

        // Після повернення книгу знову можна видати
        Console.WriteLine("Повторна спроба: Максим бере «Тигролови» після повернення");
        try
        {
            library.BorrowBook("Тигролови", "Максим");
            Console.WriteLine("  ✔ Видано. Книги Максима: " + string.Join(", ", library.GetUserBooks("Максим")));
        }
        catch (BookNotAvailableException ex)
        {
            Console.WriteLine($"  ✘ {ex.Message}");
        }
        Console.WriteLine();

        // ---------- 3. FindBook: Result<Book>, Failure(string) і Failure(Exception) ----------
        Console.WriteLine("=== 3. FindBook: Result<Book> ===");
        foreach (string? title in new[] { "кобзар", "Енеїда", null })
        {
            Result<Book> found = library.FindBook(title!);
            Console.WriteLine(found.IsSuccess
                ? $"  ✔ «{found.Value!.Title}» ({found.Value.Author}): доступно {found.Value.AvailableCopies} з {found.Value.TotalCopies}"
                : $"  ✘ {found.Error}");
        }
        Console.WriteLine();

        // ---------- 4. Стандартні конструктори винятку ----------
        Console.WriteLine("=== 4. Стандартні конструктори BookNotAvailableException ===");
        var e1 = new BookNotAvailableException();
        var e2 = new BookNotAvailableException("Бібліотека зачинена на санітарний день.");
        var e3 = new BookNotAvailableException("Не вдалося перевірити наявність.", new TimeoutException("Каталог не відповідає"));
        Console.WriteLine($"  ()               → {e1.Message}");
        Console.WriteLine($"  (message)        → {e2.Message}");
        Console.WriteLine($"  (message, inner) → {e3.Message} / Inner: {e3.InnerException?.GetType().Name}");
    }
}

/*
 * Контрольні запитання
 *
 * 1. Власні винятки дозволяють перехоплювати саме помилки предметної області (catch
 *    (BookNotAvailableException)), а не всі підряд, і передавати контекст у властивостях
 *    (BookTitle, UserName, ExistsInCatalog), а не лише в тексті повідомлення.
 * 2. Без параметрів; (string message); (string message, Exception innerException).
 * 3. Result<T> — об'єкт, що містить або успішне значення, або опис помилки. Доцільний для
 *    очікуваних неуспіхів (валідація, "не знайдено", "не брав цю книгу"): він дешевший за виняток,
 *    видний у сигнатурі методу і змушує викликача перевірити IsSuccess.
 * 4. Винятки — для ситуацій, коли метод не може виконати контракт (видати книгу, якої немає),
 *    і для технічних збоїв. Result — для передбачуваних варіантів відповіді (повернення книги,
 *    яку читач не брав; пошук без результату).
 */
