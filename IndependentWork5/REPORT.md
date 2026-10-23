# Самостійна робота №5. Приклади поліморфізму в .NET

У проєкті досліджено три сценарії. У кожному **один і той самий клієнтський код** (`Console.WriteLine`, `foreach`, `ProcessStream`) працює з різними реалізаціями.

Як запустити: `dotnet run` у папці `IndependentWork5`. Приклад виводу наведено наприкінці звіту.

---

## Сценарій 1. Поліморфізм через `System.Object`

Будь-який тип у C# неявно успадковується від `System.Object`, тому отримує віртуальні методи `ToString()`, `Equals()`, `GetHashCode()`. Класи `Product`, `Customer`, `Order` перевизначають `ToString()`:

```csharp
public class Customer
{
    ...
    public override string ToString() => $"Customer: {FullName} <{Email}>";
}
```

Об'єкти різних типів кладемо в одну колекцію `List<object>`:

```csharp
List<object> objects = new List<object>
{
    customer, keyboard, order,
    new Warehouse(),   // ToString() не перевизначено
    2026,              // int
    "просто рядок"
};

foreach (object obj in objects)
{
    Console.WriteLine($"{obj.GetType().Name,-10} -> {obj}");
}
```

**Яка реалізація `ToString()` викликається?** Та, що належить **фактичному** (runtime) типу об'єкта, а не типу змінної (`object`). Це пізнє зв'язування: CLR шукає метод у таблиці віртуальних методів конкретного класу.
- `Customer`, `Product`, `Order` викликають свої перевизначені версії;
- `Warehouse` не перевизначив метод, тому спрацьовує базова `Object.ToString()` і виводить повне ім'я типу `IndependentWork5.Warehouse`;
- `int` і `string` мають власні перевизначення в .NET.

**Переваги:** `Console.WriteLine`, інтерполяція рядків і відладчик працюють з будь-яким об'єктом і не знають про наші класи. Достатньо перевизначити один метод, і всі ці інструменти автоматично показують змістовну інформацію.

---

## Сценарій 2. Поліморфізм через `IEnumerable<T>`

Клас `MyCustomCollection` містить приватний `List<string>` і реалізує `IEnumerable<string>`:

```csharp
public class MyCustomCollection : IEnumerable<string>
{
    private readonly List<string> _items = new List<string>();

    public void Add(string item) => _items.Add(item);

    public IEnumerator<string> GetEnumerator()
    {
        foreach (string item in _items)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

- Для `foreach` достатньо методу `GetEnumerator()`. Компілятор розгортає цикл у `MoveNext()` / `Current`.
- `yield return` автоматично генерує клас-перечислювач, тому писати його вручну не треба.
- Неузагальнений `IEnumerable.GetEnumerator()` реалізовано явно, і він просто делегує роботу узагальненому.

Поліморфізм видно в методі, що приймає інтерфейс:

```csharp
private static void PrintAll(string title, IEnumerable<string> items) { ... }

PrintAll("MyCustomCollection", languages);
PrintAll("string[]", array);
PrintAll("List<string>", list);
```

Крім того, до нашої колекції без жодного додаткового коду застосовуються всі методи LINQ (`languages.Where(...)`), бо LINQ написаний для `IEnumerable<T>`.

**Переваги:** внутрішня структура (`List<string>`) прихована. Колекцію можна перебирати тим самим `foreach` і обробляти тими самими методами, що й масиви чи `List<T>`. Внутрішнє сховище можна замінити (на масив, файл, БД), і клієнтський код від цього не зміниться.

---

## Сценарій 3. Поліморфізм через абстрактний клас `Stream`

`System.IO.Stream` – абстрактний клас з абстрактними членами `Read`, `Write`, `Seek`, `Length`. `FileStream` реалізує їх для файлу на диску, `MemoryStream` для масиву байтів у пам'яті.

```csharp
private static void ProcessStream(Stream stream)
{
    Console.WriteLine($"ProcessStream отримав: {stream.GetType().Name}");
    Console.WriteLine($"  Length = {stream.Length} байт, CanRead = {stream.CanRead}, CanSeek = {stream.CanSeek}");

    byte[] buffer = new byte[16];
    int read = stream.Read(buffer, 0, buffer.Length);
    Console.WriteLine($"  Прочитано {read} байт: \"{Encoding.UTF8.GetString(buffer, 0, read)}\"");
}

using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
{
    ProcessStream(fileStream);
}

using (MemoryStream memoryStream = new MemoryStream(bytes))
{
    ProcessStream(memoryStream);
}
```

Тестовий файл `test-data.txt` створюється програмою через `File.WriteAllText` у каталозі проєкту, тож зовнішніх залежностей немає.

`ProcessStream` нічого не знає про файли чи пам'ять. Виклик `stream.Read(...)` у першому випадку звертається до ОС і читає з диска, у другому просто копіює байти з масиву. Вибір реалізації відбувається під час виконання.

**Переваги:** один метод обробки працює з файлами, пам'яттю, мережею (`NetworkStream`), стисненням (`GZipStream`) і шифруванням (`CryptoStream`). Потоки можна навіть вкладати один в одний (декоратор): `new GZipStream(new FileStream(...))`. Код стає перевикористовуваним і легко тестується, бо в тестах замість файлу можна підставити `MemoryStream`.

---

## Загальні висновки

| Сценарій | Механізм | Хто викликає | Що отримуємо |
|---|---|---|---|
| `Object.ToString()` | віртуальний метод базового класу | `Console.WriteLine`, інтерполяція | змістовний вивід будь-якого об'єкта |
| `IEnumerable<T>` | інтерфейс | `foreach`, LINQ, `string.Join` | однаковий перебір будь-яких колекцій |
| `Stream` | абстрактний клас | `ProcessStream`, `StreamReader` тощо | один код для файлів, пам'яті, мережі |

Поліморфізм дає змогу писати код проти **абстракції** (базового класу або інтерфейсу), а не проти конкретної реалізації. Тому код гнучкий (нові типи додаються без його зміни), перевикористовуваний (один метод на всі типи) і зручний для тестування.

## Запуск і приклад виводу

```
cd IndependentWork5
dotnet run
```

```
=== Сценарій 1. Поліморфізм через System.Object ===
Customer   -> Customer: Андрій Мельник <andrii@example.com>
Product    -> Product: Клавіатура, ціна 1 500,00 грн
Order      -> Order #42: Андрій Мельник, товарів: 2, сума 2 150,00 грн
Warehouse  -> IndependentWork5.Warehouse
Int32      -> 2026
String     -> просто рядок

=== Сценарій 2. Поліморфізм через IEnumerable<string> ===
foreach по MyCustomCollection:
  - C#
  - Java
  - Python
  - Go
PrintAll(MyCustomCollection): C# | Java | Python | Go
PrintAll(string[]): яблуко | груша
PrintAll(List<string>): один | два | три
LINQ по MyCustomCollection (довжина > 2): Java, Python

=== Сценарій 3. Поліморфізм через абстрактний клас Stream ===
ProcessStream отримав: FileStream
  Length = 50 байт, CanRead = True, CanSeek = True
  Прочитано 16 байт: "Hello from FileS"
  Перші байти (hex): 48-65-6C-6C-6F-20-66-72
ProcessStream отримав: MemoryStream
  Length = 51 байт, CanRead = True, CanSeek = True
  Прочитано 16 байт: "Hello from Memor"
  Перші байти (hex): 48-65-6C-6C-6F-20-66-72
```

## Відповіді на контрольні запитання

1. **Як поліморфізм проявляється при роботі з `List<object>`?** Елементи зберігаються як `object`, але `ToString()` для кожного викликається з фактичного типу. Тому `Customer` друкується як `Customer: ...`, а `Warehouse` без перевизначення як `IndependentWork5.Warehouse`.
2. **Що потрібно реалізувати в MyCustomCollection, щоб її можна було перебирати через foreach?** Метод `GetEnumerator()`, що повертає `IEnumerator<string>` (тут через `yield return`). Формально досить і самого методу, але реалізація `IEnumerable<string>` (разом з неузагальненим `IEnumerable.GetEnumerator()`) додає підтримку LINQ і передачу в будь-які методи, що приймають `IEnumerable<string>`.
3. **Чому метод ProcessStream(Stream stream) є прикладом поліморфної роботи з абстрактним класом?** Його параметр має тип абстрактного класу, а викликається він з `FileStream` і `MemoryStream`. `Read`, `Length` та інші члени виконуються за реалізацією конкретного нащадка, яку обирають під час виконання.
4. **Які переваги дає єдиний інтерфейс роботи для різних типів потоків і колекцій?** Код пишеться один раз і працює з усіма реалізаціями. Нові реалізації не потребують змін у клієнтському коді (принцип Open/Closed). Реалізацію легко підмінити, наприклад у тестах. Код залежить від абстракції, а не від деталей.
