# Самостійна робота №3. Аналіз інкапсуляції в open-source проєкті

**Тема:** аналіз інкапсуляції в open-source проєктах і практики валідації полів.

## 1. Обраний проєкт

- **Назва:** .NET Runtime (`dotnet/runtime`). Це вихідний код стандартної бібліотеки .NET: `System.Collections`, `System.ComponentModel.DataAnnotations` та інших.
- **Посилання на GitHub:** https://github.com/dotnet/runtime
- **Версія коду:** коміт [`9da2195`](https://github.com/dotnet/runtime/tree/9da219572c111bc73faa1a8dd712115a111d9a71). Посилання нижче прив'язані до цього коміту, щоб номери рядків не «з'їхали» після нових змін у репозиторії.

**Чому цей проєкт.** Цими класами ми користуємося щодня (`List<T>`, `Version`, атрибути валідації). Репозиторій має чітку структуру `src/libraries/<збірка>/src/...`. Код написаний командою Microsoft за суворими правилами, тому добре показує «еталонну» інкапсуляцію.

**Проаналізовані файли:**

| Клас | Файл |
|---|---|
| `List<T>` | [System.Private.CoreLib/src/System/Collections/Generic/List.cs](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs) |
| `Version` | [System.Private.CoreLib/src/System/Version.cs](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.Private.CoreLib/src/System/Version.cs) |
| `RangeAttribute` | [System.ComponentModel.Annotations/.../DataAnnotations/RangeAttribute.cs](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/RangeAttribute.cs) |

## 2. Аналіз інкапсуляції

### Клас: `List<T>`

- **Опис класу:** динамічний масив, тобто список, який сам збільшується при додаванні елементів. Усередині він зберігає звичайний масив `T[]` і стежить, скільки комірок цього масиву реально зайнято.

- **Поля** ([рядки 23–30](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs#L23-L30)):

  ```csharp
  private const int DefaultCapacity = 4;

  internal T[] _items; // Do not rename (binary serialization) ...
  internal int _size;  // Do not rename (binary serialization) ...
  internal int _version; // Do not rename (binary serialization)

  private static readonly T[] s_emptyArray = new T[0];
  ```

  Публічних полів у класі немає. Внутрішній масив `_items`, лічильник `_size` і лічильник змін `_version` мають модифікатор `internal`. Отже, вони видимі лише всередині збірки `System.Private.CoreLib` (там, наприклад, їх використовують швидкі шляхи в `CollectionsMarshal`). Для нашого коду ці поля повністю приховані. Константа `DefaultCapacity` і порожній масив `s_emptyArray` позначені `private`.

- **Властивості** ([рядки 97–127](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs#L97-L127)):

  ```csharp
  public int Capacity
  {
      get => _items.Length;
      set
      {
          if (value < _size)
          {
              ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_SmallCapacity);
          }
          if (value != _items.Length)
          {
              // ... створюється новий масив і в нього копіюються елементи
          }
      }
  }

  // Read-only property describing how many elements are in the List.
  public int Count => _size;
  ```

  - `Capacity` – властивість для читання й запису, і її set містить логіку. Ємність не можна зробити меншою за кількість наявних елементів. Якщо ж ємність змінилась, клас сам виділяє новий масив і копіює дані.
  - `Count` – **read-only** властивість (тільки get, записана як expression-bodied член `=>`). Кількість елементів змінюється тільки через методи `Add`, `Remove`, `Clear`, тому список не може опинитися в неузгодженому стані.
  - Автоматичних властивостей (`{ get; set; }`) у цьому класі немає, бо кожна властивість спирається на внутрішній масив.

- **Індексатор** ([рядки 143–163](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs#L143-L163)):

  ```csharp
  public T this[int index]
  {
      get
      {
          // Following trick can reduce the range check by one
          if ((uint)index >= (uint)_size)
          {
              ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
          }
          return _items[index];
      }
      set
      {
          if ((uint)index >= (uint)_size)
          {
              ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
          }
          _items[index] = value;
          _version++;
      }
  }
  ```

  Індексатор дає звертатися до списку як до масиву (`list[0]`), але перевіряє індекс саме відносно `_size`, а не довжини внутрішнього масиву. Без цієї перевірки можна було б прочитати «сміття» з незайнятих комірок. Сетер також збільшує `_version`. Завдяки цьому `foreach` помічає, що колекцію змінили під час перебору, і викидає `InvalidOperationException`.

### Клас: `Version`

- **Опис класу:** номер версії у форматі `major.minor[.build[.revision]]`, наприклад `8.0.1`. Клас незмінний (immutable): після створення значення змінити не можна.

- **Поля** ([рядки 26–29](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.Private.CoreLib/src/System/Version.cs#L26-L29)):

  ```csharp
  private readonly int _Major; // Do not rename (binary serialization)
  private readonly int _Minor; // Do not rename (binary serialization)
  private readonly int _Build; // Do not rename (binary serialization)
  private readonly int _Revision; // Do not rename (binary serialization)
  ```

  Усі поля `private readonly`. Їх можна присвоїти тільки в конструкторі, а далі вони незмінні навіть для коду всередині класу.

- **Властивості** ([рядки 100–106](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.Private.CoreLib/src/System/Version.cs#L100-L106)):

  ```csharp
  public int Major => _Major;
  public int Minor => _Minor;
  public int Build => _Build;
  public int Revision => _Revision;
  ```

  Усі властивості **тільки для читання**. Клас `sealed`, тому успадкувати його й «зламати» незмінність теж не вийде. Такий об'єкт безпечно передавати між потоками і використовувати як ключ у `Dictionary`.

- **Перевантажені оператори** ([рядки 466–503](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.Private.CoreLib/src/System/Version.cs#L466-L503)):

  ```csharp
  public static bool operator ==(Version? v1, Version? v2)
  {
      if (v2 is null)
      {
          return v1 is null;
      }
      return ReferenceEquals(v2, v1) || v2.Equals(v1);
  }

  public static bool operator <(Version? v1, Version? v2)
  {
      if (v1 is null)
      {
          return v2 is not null;
      }
      return v1.CompareTo(v2) < 0;
  }

  public static bool operator >(Version? v1, Version? v2) => v2 < v1;
  ```

  Оператори `==`, `!=`, `<`, `<=`, `>`, `>=` дають писати природні вирази на кшталт `if (current < required)`. Вся логіка порівняння живе в одному методі `CompareTo`, а оператори лише делегують йому роботу. Окремо коректно обробляється `null`.

### Клас: `RangeAttribute`

- **Опис класу:** атрибут валідації з `System.ComponentModel.DataAnnotations`. Ставиться над властивістю моделі (`[Range(1, 100)]`) і перевіряє, що значення лежить у діапазоні. Його використовують ASP.NET Core, Blazor, WPF тощо.

- **Поля і властивості** ([рядки 67–106](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/RangeAttribute.cs#L67-L106)):

  ```csharp
  public object Minimum { get; private set; }
  public object Maximum { get; private set; }
  public bool MinimumIsExclusive { get; set; }
  public bool MaximumIsExclusive { get; set; }
  public Type OperandType { get; }
  private Func<object, object?>? Conversion { get; set; }
  ```

  Тут видно відразу кілька видів **автоматичних властивостей**:
  - `Minimum`, `Maximum` – `{ get; private set; }`. Читати їх може будь-хто, а змінювати лише сам клас (у конструкторі та методі `Initialize`).
  - `OperandType` – `{ get; }`, тобто справжній read-only. Значення задається тільки в конструкторі.
  - `MinimumIsExclusive`, `MaximumIsExclusive` – звичайні `{ get; set; }`. Їх задають іменованими аргументами атрибута: `[Range(0, 10, MinimumIsExclusive = true)]`.
  - `Conversion` – **приватна** автовластивість. Функція перетворення значення є деталлю реалізації і назовні не видна.

## 3. Практики валідації у властивостях

- **Приклад 1: валідація в set-аксесорі (`List<T>.Capacity`).** Перевірка стоїть прямо в сетері: якщо нова ємність менша за кількість елементів, викидається виняток. Цей спосіб найпростіший і найнадійніший, бо обійти перевірку неможливо.

  ```csharp
  set
  {
      if (value < _size)
      {
          ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_SmallCapacity);
      }
      ...
  }
  ```

  Варто звернути увагу на `ThrowHelper`: виняток створюється в окремому методі. Код сетера від цього менший, і JIT‑компілятор може вбудувати (inline) його у місце виклику. Такий прийом корисний для коду, від якого залежить продуктивність.

- **Приклад 2: валідація аргументів у конструкторі (`Version`)** ([рядки 31–42](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.Private.CoreLib/src/System/Version.cs#L31-L42)). Оскільки властивості read-only і сетерів немає, перевіряти треба там, де значення потрапляють в об'єкт, тобто в конструкторі:

  ```csharp
  public Version(int major, int minor, int build, int revision)
  {
      ArgumentOutOfRangeException.ThrowIfNegative(major);
      ArgumentOutOfRangeException.ThrowIfNegative(minor);
      ArgumentOutOfRangeException.ThrowIfNegative(build);
      ArgumentOutOfRangeException.ThrowIfNegative(revision);

      _Major = major;
      ...
  }
  ```

  Тут використано сучасні статичні методи‑хелпери `ArgumentOutOfRangeException.ThrowIfNegative` (з'явилися в .NET 8). Один рядок замінює конструкцію `if (...) throw new ...`, а ім'я параметра в повідомленні підставляється автоматично через `CallerArgumentExpression`.

- **Приклад 3: декларативна валідація атрибутами (`RangeAttribute.IsValid`)** ([рядки 131–170](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/RangeAttribute.cs#L131-L170)):

  ```csharp
  public override bool IsValid(object? value)
  {
      SetupConversion();

      // Automatically pass if value is null or empty. RequiredAttribute should be used to assert a value is not empty.
      if (value is null or string { Length: 0 })
      {
          return true;
      }

      object? convertedValue;
      try
      {
          convertedValue = Conversion!(value);
      }
      catch (FormatException)
      {
          return false;
      }
      // ... InvalidCastException, NotSupportedException, OverflowException

      var min = (IComparable)Minimum;
      var max = (IComparable)Maximum;
      return
          (MinimumIsExclusive ? min.CompareTo(convertedValue) < 0 : min.CompareTo(convertedValue) <= 0) &&
          (MaximumIsExclusive ? max.CompareTo(convertedValue) > 0 : max.CompareTo(convertedValue) >= 0);
  }
  ```

  Підхід принципово інший. Властивість моделі залишається простою (`public int Age { get; set; }`) і нічого не перевіряє, а правило записується атрибутом `[Range(18, 120)]`. Перевірку запускає зовнішній код через `Validator.TryValidateObject` або MVC під час прив'язування моделі. Метод **не викидає винятків** для неправильних даних, а повертає `false`, і з таких результатів формується список помилок для користувача. Винятком (`InvalidOperationException` в `Initialize`, [рядки 108–122](https://github.com/dotnet/runtime/blob/9da219572c111bc73faa1a8dd712115a111d9a71/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/RangeAttribute.cs#L108-L122)) закінчується тільки помилка програміста, наприклад `Minimum > Maximum`.

- **Висновки щодо валідації:**

  | Підхід | Де трапився | Переваги | Недоліки |
  |---|---|---|---|
  | `if` + виняток у `set` | `List<T>.Capacity`, індексатор | неможливо обійти, об'єкт завжди коректний | на одну помилку один виняток, тому незручно збирати всі помилки форми |
  | Перевірка в конструкторі + read-only | `Version` | незмінний і завжди валідний об'єкт | треба створювати новий об'єкт, щоб «змінити» значення |
  | Атрибути DataAnnotations | `RangeAttribute` | правила оголошуються декларативно, можна зібрати всі помилки разом, фреймворки підтримують це «з коробки» | об'єкт може тимчасово бути невалідним, перевірку треба викликати явно |

  Перші два підходи підходять для доменних класів і бібліотек, де невалідний стан недопустимий. Третій краще використовувати для моделей вводу (форми, DTO, API‑запити), де користувачу треба показати всі помилки одразу.

## 4. Загальні висновки

1. У всіх трьох класах **немає жодного публічного поля**. Стан закритий модифікаторами `private`/`internal`, а доступ дають властивості, індексатори й методи. Це головна ознака дотриманої інкапсуляції.
2. Рівень доступу підібрано точно під задачу: `get; private set;` у `RangeAttribute`, лише `get` у `Version`, `internal` поля в `List<T>` для оптимізацій у межах однієї збірки. Видимість мінімальна, але достатня.
3. Незмінність (`readonly` поля + read-only властивості + `sealed`) у `Version` – найсильніша форма інкапсуляції. Об'єкт неможливо зіпсувати після створення.
4. Індексатор `List<T>` і оператори `Version` роблять API зручним (`list[i]`, `v1 < v2`), але не розкривають внутрішньої будови.
5. Валідацію розміщують там, де дані потрапляють в об'єкт: у сетері, конструкторі або атрибуті. Вибір способу залежить від того, чи має об'єкт **завжди** бути коректним, чи треба лише перевірити введення користувача.

Загалом підходи в `dotnet/runtime` ефективні й доцільні. Код поєднує сувору інкапсуляцію з продуктивністю (`ThrowHelper`, трюк `(uint)index >= (uint)_size`, який замінює дві перевірки однією) і залишається зрозумілим.

## Відповіді на контрольні запитання

1. **За якими ознаками можна зробити висновок, що в класі дотримано інкапсуляції?** Поля приватні або `internal`, і немає публічних полів. Зміна стану відбувається тільки через властивості та методи, які перевіряють дані. Видимість мінімальна (`private set`, read-only). Внутрішня структура (наприклад, масив `_items` у `List<T>`) не видна користувачу класу.
2. **Які підходи до валідації ви зустріли в реальному коді?** Перевірка з винятком у `set`-аксесорі (`List<T>.Capacity`). Перевірка аргументів конструктора через `ArgumentOutOfRangeException.ThrowIfNegative` (`Version`). Декларативна валідація атрибутами DataAnnotations (`RangeAttribute.IsValid`).
3. **Чому для аналізу важливо наводити посилання на конкретні класи та фрагменти коду?** Так висновки можна перевірити, бо будь-хто відкриє той самий код. Посилання на конкретний коміт гарантує, що рядки не зміняться. Без фрагментів коду аналіз стає формальним і нічим не підтвердженим.
4. **Які висновки щодо якості проєктування ви зробили після порівняння 2–3 класів?** Кожен клас обирає рівень захисту під своє призначення. `Version` незмінний, бо він значення. `List<T>` змінюваний, але з контролем інваріантів (`_size ≤ Capacity`). `RangeAttribute` відділяє правило перевірки від даних. Спільне в усіх трьох: жодних публічних полів, валідація на вході й зручний публічний API.
