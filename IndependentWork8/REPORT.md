# Самостійна робота №8. Dictionary<TKey, TValue> та HashSet<T> у бібліотеках .NET

## Частина 1. Аналіз використання у відкритих проєктах

Досліджуваний проєкт: **Newtonsoft.Json** (Json.NET), найпопулярніша бібліотека серіалізації JSON для .NET.
Репозиторій: https://github.com/JamesNK/Newtonsoft.Json. Посилання прив'язані до коміту [`52fa3ae`](https://github.com/JamesNK/Newtonsoft.Json/tree/52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa).

### Приклад 1. `Dictionary<TKey, TValue>`: `BidirectionalDictionary` для `$id` / `$ref`

Файли:
- [Utilities/BidirectionalDictionary.cs, рядки 33–95](https://github.com/JamesNK/Newtonsoft.Json/blob/52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa/Src/Newtonsoft.Json/Utilities/BidirectionalDictionary.cs#L33-L95)
- [Serialization/DefaultReferenceResolver.cs, рядки 59–81](https://github.com/JamesNK/Newtonsoft.Json/blob/52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa/Src/Newtonsoft.Json/Serialization/DefaultReferenceResolver.cs#L59-L81)
- [Serialization/JsonSerializerInternalBase.cs, рядки 36–48 і 65–81](https://github.com/JamesNK/Newtonsoft.Json/blob/52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa/Src/Newtonsoft.Json/Serialization/JsonSerializerInternalBase.cs#L36-L81)

```csharp
internal class BidirectionalDictionary<TFirst, TSecond>
{
    private readonly IDictionary<TFirst, TSecond> _firstToSecond;
    private readonly IDictionary<TSecond, TFirst> _secondToFirst;
    ...
    _firstToSecond = new Dictionary<TFirst, TSecond>(firstEqualityComparer);
    _secondToFirst = new Dictionary<TSecond, TFirst>(secondEqualityComparer);

    public bool TryGetByFirst(TFirst first, out TSecond? second)
        => _firstToSecond.TryGetValue(first, out second);

    public bool TryGetBySecond(TSecond second, out TFirst? first)
        => _secondToFirst.TryGetValue(second, out first);
}
```

- **Контекст.** При `PreserveReferencesHandling` Json.NET записує однаковий об'єкт один раз з `"$id": "1"`, а далі лише як `"$ref": "1"`. Серіалізатору треба швидко відповідати на питання «чи бачив я вже цей об'єкт і який у нього id?» (`GetReference`). Десеріалізатору потрібне обернене: «який об'єкт має id "1"?» (`ResolveReference`).
- **Чому саме ця колекція.** Пошук в обидва боки за ключем. Два `Dictionary` дають O(1) для обох напрямків. Зі `List<(string, object)>` кожен пошук коштував би O(n), а для графа з тисяч об'єктів серіалізація стала б O(n²).
- **Складність.** `TryGetByFirst` / `TryGetBySecond` – O(1) в середньому. `Set` – O(1): дві перевірки `TryGetValue` + два `Add`.
- **Особливі вимоги: custom comparer.** Для словника `object → id` використовується `ReferenceEqualsEqualityComparer`:

  ```csharp
  private class ReferenceEqualsEqualityComparer : IEqualityComparer<object>
  {
      bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);

      int IEqualityComparer<object>.GetHashCode(object obj)
          => RuntimeHelpers.GetHashCode(obj);   // хеш за посиланням
  }
  ```

  Коментар у коді пояснює причину: об'єкт змінюється під час десеріалізації, тому його `GetHashCode()` може змінитись (mutable hashcode), і словник «загубить» ключ. Порівняння за посиланням також розрізняє два різні, але «рівні» за `Equals` об'єкти.

### Приклад 2. `HashSet<T>`: злиття масивів JSON без дублікатів

Файл: [Linq/JContainer.cs, рядки 1212–1252](https://github.com/JamesNK/Newtonsoft.Json/blob/52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa/Src/Newtonsoft.Json/Linq/JContainer.cs#L1212-L1252)

```csharp
case MergeArrayHandling.Union:
#if HAVE_HASH_SET
    HashSet<JToken> items = new HashSet<JToken>(target, EqualityComparer);

    foreach (object item in content)
    {
        JToken contentItem = CreateFromContent(item);

        if (items.Add(contentItem))
        {
            target.Add(contentItem);
        }
    }
```

- **Контекст.** `JArray.Merge(other, new JsonMergeSettings { MergeArrayHandling = Union })` об'єднує два JSON‑масиви і додає лише ті елементи, яких ще немає.
- **Чому `HashSet`.** Потрібна лише перевірка «чи є такий елемент», а значення-пари не потрібне. `HashSet.Add` повертає `false` для дубліката, тому перевірка і вставка відбуваються **за одну операцію**. Перевірка через `List.Contains` для кожного елемента дала б O(n·m).
- **Складність.** Побудова множини з `target` – O(n). Кожен `Add` – O(1). Разом O(n + m) замість O(n·m).
- **Custom comparer.** `JToken.EqualityComparer` (`JTokenEqualityComparer`) порівнює JSON‑вузли **за значенням** (`DeepEquals`). Два різні об'єкти `{"a":1}` вважаються однаковими. Без цього компаратора `HashSet` порівнював би посилання, і дублікати не відфільтрувались би.

### Приклад 3 (комбінований). `Dictionary` + `HashSet` в одному методі

Той самий метод `MergeEnumerableContent` має другу гілку для старих платформ (.NET 2.0), де `HashSet<T>` ще не існувало:

```csharp
#else
    Dictionary<JToken, bool> items = new Dictionary<JToken, bool>(EqualityComparer);
    foreach (JToken t in target)
    {
        items[t] = true;
    }

    foreach (object item in content)
    {
        JToken contentItem = CreateFromContent(item);

        if (!items.ContainsKey(contentItem))
        {
            items[contentItem] = true;
            target.Add(contentItem);
        }
    }
#endif
```

- **Контекст.** Та сама задача (Union без дублікатів), але `HashSet` недоступний, тому множину **емулюють** словником `Dictionary<JToken, bool>`, де значення `bool` фіктивне.
- **Що показує порівняння.** `HashSet<T>` фактично є `Dictionary` без значень: та сама хеш-таблиця і та сама O(1)‑складність. Проте `HashSet` економить пам'ять (немає поля `bool` у кожному записі), має чистіший API (`Add` повертає `bool`, тож не потрібні окремі `ContainsKey` + присвоєння) і операції над множинами (`UnionWith`, `IntersectWith`).
- **Особливість.** В обох гілках передається **той самий** `EqualityComparer`, тобто custom comparer однаково підтримують обидві колекції.

### Додатковий приклад. `Dictionary` у потокобезпечному кеші

[Utilities/ThreadSafeStore.cs, рядки 39–108](https://github.com/JamesNK/Newtonsoft.Json/blob/52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa/Src/Newtonsoft.Json/Utilities/ThreadSafeStore.cs#L39-L108) кешує результати рефлексії (контракти типів). На нових платформах тут використовується `ConcurrentDictionary.GetOrAdd`. На старих застосовано `Dictionary` + `lock` + прийом **copy-on-write**: при додаванні створюється *новий* словник `new Dictionary<TKey, TValue>(_store)` і підміняється посилання. Читання тоді відбувається без блокування. Це наочно показує підводний камінь: звичайний `Dictionary` **не потокобезпечний** для одночасного запису.

---

## Частина 2. Практична реалізація

Проєкт `IndependentWork8` містить три класи:

| Клас | Колекції | Ключові операції |
|---|---|---|
| `FileMetadataCache` | `Dictionary<string, FileMetadata>` з `StringComparer.OrdinalIgnoreCase` | `Add`, `Get` (через `TryGetValue`), `Remove`, `Contains`, `IsChanged` |
| `TagManager` | `Dictionary<string, HashSet<string>>` | `AddTag`, `RemoveTag`, `GetAllUniqueTags` (`UnionWith`), `FindCommonTags` (`IntersectWith`), `FindUniqueTags` (`ExceptWith`) |
| `SearchIndex` | `Dictionary<string, HashSet<int>>` | `AddDocument`, `Search`, `SearchMultiple` (`IntersectWith`) |

Важливі рішення:
- шляхи до файлів і теги порівнюються **без урахування регістру** через custom comparer, тому `REPORT.DOCX` знаходить `Report.docx`;
- методи `GetTags` / `Search` повертають **копію** множини. Інакше виклик `IntersectWith` у `FindCommonTags` зіпсував би дані всередині менеджера;
- у `SearchMultiple` цикл зупиняється, щойно перетин став порожнім.

Приклад виводу програми наведено в `README.md`.

---

## Частина 3. Порівняльна таблиця

| Операція | `List<T>` | `Dictionary<TKey,TValue>` | `HashSet<T>` | Коли використовувати |
|---|---|---|---|---|
| Додавання | O(1) амортизовано (в кінець), O(n) у середину | O(1) в середньому | O(1) в середньому | List – коли важливий порядок; Dictionary/HashSet – коли важливий швидкий пошук |
| Пошук за значенням | O(n) (`Contains`, `IndexOf`) | N/A (`ContainsValue` – O(n)) | O(1) (`Contains`) | HashSet – для частих перевірок «чи є елемент» |
| Пошук за ключем | N/A (за індексом – O(1)) | O(1) (`TryGetValue`) | N/A | Dictionary – коли дані знаходять за ідентифікатором |
| Видалення | O(n) (зсув елементів) | O(1) | O(1) | Dictionary/HashSet – для частих видалень |
| Перевірка наявності | O(n) | O(1) (`ContainsKey`) | O(1) | HashSet/Dictionary замість `List.Contains` у циклах |
| Операції над множинами | O(n·m) вручну | – | O(n) (`UnionWith`, `IntersectWith`, `ExceptWith`) | HashSet – теги, права доступу, унікальні значення |

O(1) для хеш-колекцій – це *середній* випадок. За поганої хеш-функції (багато колізій) операції деградують до O(n). Крім того, `Dictionary` і `HashSet` не гарантують порядку елементів.

---

## Частина 4. Звіт

### 1. Результати аналізу

У Newtonsoft.Json хеш-колекції використовуються там, де потрібен швидкий пошук або унікальність:
- `Dictionary` – для відображень «ключ → значення» (id ↔ об'єкт, тип → контракт);
- `HashSet` – для відсіювання дублікатів при злитті масивів;
- `Dictionary<T, bool>` – як замінник `HashSet` на старих платформах.

В усіх прикладах явно передається **custom comparer** (`ReferenceEqualsEqualityComparer`, `JTokenEqualityComparer`). Отже, в реальному коді питання «що вважати рівним» не менш важливе, ніж вибір самої колекції.

### 2. Висновки про вибір колекції

**Коли `Dictionary` краще за `List`?** Коли дані шукають за ключем (id, ім'я, шлях) і таких пошуків багато. `List` вимагає перебору O(n), а `Dictionary` дає O(1). Поріг відчутний уже на десятках елементів, якщо пошук відбувається в циклі.

**Коли `HashSet` є оптимальним вибором?**
- потрібно зберігати лише унікальні значення (теги, відвідані вузли графа, оброблені id);
- часто треба перевіряти «чи є елемент»;
- потрібні операції над множинами: об'єднання, перетин, різниця.

**Підводні камені:**
1. **Змінювані ключі.** Якщо змінити поле, від якого залежить `GetHashCode()`, після додавання в колекцію, елемент «загубиться». Саме через це Json.NET порівнює за посиланням.
2. **`Equals` і `GetHashCode` мають бути узгоджені.** Рівні об'єкти мусять мати однаковий хеш, інакше `HashSet` пропустить дублікати.
3. **Регістр рядків.** За замовчуванням `"Hello" != "hello"`. Потрібен `StringComparer.OrdinalIgnoreCase`.
4. **Індексатор кидає виняток.** `dict[key]` викидає `KeyNotFoundException`, тому для необов'язкових ключів краще `TryGetValue`.
5. **Порядок не гарантується.** Для виводу потрібен `OrderBy` або `SortedDictionary`/`SortedSet`.
6. **Потокобезпечність.** Звичайні колекції не можна одночасно змінювати з кількох потоків. Тоді потрібні `ConcurrentDictionary` або `lock`.
7. **Пам'ять.** Хеш-таблиця займає більше пам'яті, ніж `List` тих самих елементів.

### 3. Приклади з власного коду

```csharp
// Пошук без урахування регістру - O(1)
cache.Get(@"c:\docs\REPORT.DOCX");    // знайде "C:\Docs\Report.docx"

// Дублікат тегу не додається
tags.AddTag("post-1", "csharp");
tags.AddTag("post-1", "CSharp");      // ігнорується

// Перетин множин - спільні теги
tags.FindCommonTags("post-1", "post-2");   // {collections, csharp}

// Інвертований індекс: AND-пошук
index.SearchMultiple("c#", "java");        // [3]
```

Повний вивід програми: `README.md`.

## Відповіді на контрольні запитання

1. **Які переваги generic-колекцій над неузагальненими структурами даних у .NET?** Безпека типів перевіряється на етапі компіляції, тому немає приведень `(string)obj` і `InvalidCastException`. Для значимих типів немає boxing/unboxing, а отже менше пам'яті й швидше. Код читабельніший, і IntelliSense підказує тип.
2. **У яких сценаріях доцільніше використовувати Dictionary<TKey, TValue>, а в яких HashSet<T>?** `Dictionary` – коли за ключем треба отримати пов'язане значення (кеш, індекс, конфігурація). `HashSet` – коли значення не потрібне, а важлива лише належність і унікальність (теги, відвідані елементи, операції над множинами).
3. **Як IEqualityComparer<T> впливає на коректність і продуктивність Dictionary та HashSet?** Він визначає, які ключі вважаються рівними (регістр, порівняння за посиланням чи за значенням), тобто від нього залежить коректність. `GetHashCode` визначає розподіл по кошиках: погана хеш-функція дає колізії, і O(1) деградує до O(n).
4. **Чому інвертований індекс зручно будувати саме на базі Dictionary<string, HashSet<int>>?** `Dictionary` за O(1) знаходить список документів для слова. `HashSet<int>` автоматично прибирає повтори (слово двічі в одному документі) і має `IntersectWith`/`UnionWith`, тому AND/OR-пошук за кількома словами – це просто перетин або об'єднання множин.
