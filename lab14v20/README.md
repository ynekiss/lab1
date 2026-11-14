# Лабораторна робота №14

**Тема:** Аналіз SRP/OCP у відкритих репозиторіях.

**Мета:** Дослідити, як принципи SRP та OCP застосовуються в реальних проєктах, і підготувати короткий практичний аналіз прикладів рефакторингу.

**Хід роботи:**
1. Обрав два open-source репозиторії на C# з різних галузей:
   - **Polly** — бібліотека відмовостійкості (Retry, Circuit Breaker, Timeout…): [App-vNext/Polly](https://github.com/App-vNext/Polly), коміт [`5f9462d`](https://github.com/App-vNext/Polly/tree/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec) (версія 8.6.4);
   - **Jellyfin** — медіасервер (бібліотека фільмів і серіалів, метадані, стрімінг): [jellyfin/jellyfin](https://github.com/jellyfin/jellyfin), коміт [`bf14e86`](https://github.com/jellyfin/jellyfin/tree/bf14e863d42cf65cf1e950e437e0055c89a26251).
2. Переглянув код і знайшов 2 приклади вдалого SRP, 2 порушення SRP і 2 приклади розширення за OCP. Посилання прив'язані до конкретних комітів, тому номери рядків не зсунуться.
3. Для кожного прикладу описав, де видно принцип, які переваги чи ризики це дає для підтримки і що зміниться при додаванні нової функції.
4. Зробив висновки і запропонував рефакторинг для найгіршого прикладу.

---

## 1. Вдале застосування SRP

### 1.1. Polly: стратегія Retry розділена на три класи

| Клас | Відповідальність |
|---|---|
| [`RetryStrategyOptions<TResult>`](https://github.com/App-vNext/Polly/blob/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec/src/Polly.Core/Retry/RetryStrategyOptions.TResult.cs#L12-L111) | лише налаштування: кількість спроб, тип затримки, `ShouldHandle`, `DelayGenerator`, `OnRetry` |
| [`RetryResilienceStrategy<T>`](https://github.com/App-vNext/Polly/blob/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec/src/Polly.Core/Retry/RetryResilienceStrategy.cs#L5-L148) | лише цикл виконання: викликати операцію, спитати `ShouldHandle`, почекати, повторити |
| [`RetryHelper`](https://github.com/App-vNext/Polly/blob/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec/src/Polly.Core/Retry/RetryHelper.cs#L16-L30) | лише математика затримки (constant / linear / exponential, jitter, `MaxDelay`) |

- **Де видно принцип:** стратегія не рахує затримку сама, а викликає `RetryHelper.GetRetryDelay(...)` ([рядок 86](https://github.com/App-vNext/Polly/blob/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec/src/Polly.Core/Retry/RetryResilienceStrategy.cs#L86)). Кожен клас має одну причину для змін: нова формула backoff — `RetryHelper`, нове налаштування — `Options`, зміна циклу — `Strategy`.
- **Переваги:** формули затримки тестуються окремо, як чисті функції, без запуску операцій. Файли невеликі (135–148 рядків).
- **Нова функція:** новий тип затримки (наприклад, «Fibonacci») зачепить лише `RetryHelper` і перелік `DelayBackoffType`, а цикл повторів не зміниться.

### 1.2. Jellyfin: окрема запланована задача `CleanActivityLogTask`

[`Emby.Server.Implementations/ScheduledTasks/Tasks/CleanActivityLogTask.cs`, рядки 15–76](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ScheduledTasks/Tasks/CleanActivityLogTask.cs#L15-L76)

- **Де видно принцип:** клас займає 76 рядків і робить одне — очищує старі записи журналу активності (`ExecuteAsync`, рядок 59) і повідомляє свій розклад за замовчуванням (`GetDefaultTriggers`, рядок 72). Запуск за розкладом, прогрес і історію виконань веде окремий [`ScheduledTaskWorker`](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ScheduledTasks/ScheduledTaskWorker.cs#L24).
- **Переваги:** логіку очищення можна змінити, не ризикуючи зламати планувальник, і навпаки.
- **Нова функція:** зміна правила «скільки днів зберігати журнал» торкнеться лише цього класу.

## 2. Порушення SRP

### 2.1. Jellyfin: `ApplicationHost` — «клас-бог» запуску сервера

[`Emby.Server.Implementations/ApplicationHost.cs`](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs) (1076 рядків) одночасно відповідає за:

| Відповідальність | Рядки |
|---|---|
| реєстрацію ~90 сервісів у DI-контейнері | [499–643](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs#L499-L643) |
| завантаження й перевірку SSL-сертифіката | [655–689](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs#L655-L689), [838–858](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs#L838-L858) |
| пошук плагінів і частин через рефлексію | [720–797](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs#L720-L797), [881–927](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs#L881-L927) |
| реакцію на зміну конфігурації (порти, перезапуск) | [798–836](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs#L798-L836) |
| побудову URL сервера для клієнтів | [930–1008](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs#L930-L1008) |

- **Ризики:** у класу п'ять різних причин для змін, тому зміни від різних людей (мережа, безпека, плагіни) потрапляють в один файл і конфліктують. Логіку сертифікатів чи побудови URL неможливо протестувати без створення всього хоста.
- **Нова функція:** щоб додати, наприклад, підтримку сертифіката зі сховища ОС, доведеться змінювати клас, який запускає весь сервер.

### 2.2. Jellyfin: `BaseItem` — сутність, яка робить усе

[`MediaBrowser.Controller/Entities/BaseItem.cs`](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Controller/Entities/BaseItem.cs) — 3145 рядків, базовий клас для фільмів, серій, пісень, папок тощо. Крім даних елемента, він містить:

- 12 статичних сервісів-локаторів (`LibraryManager`, `ItemRepository`, `FileSystem`, `UserDataManager`…) — [рядки 512–534](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Controller/Entities/BaseItem.cs#L512-L534);
- права доступу і видалення: `CanDelete`, `IsAuthorizedToDelete` — [868–921](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Controller/Entities/BaseItem.cs#L868-L921);
- батьківський контроль і видимість для користувача: `IsParentalAllowed`, `IsVisible` — [1919–2095](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Controller/Entities/BaseItem.cs#L1919-L2095);
- оновлення метаданих: `RefreshMetadata` — [1553–1564](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Controller/Entities/BaseItem.cs#L1553-L1564);
- позначки перегляду і роботу із зображеннями: `MarkPlayed`, `SetImage`, `ValidateImages` — [2261–2449](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Controller/Entities/BaseItem.cs#L2261-L2449).

- **Ризики:** будь-яка зміна в правилах доступу, метаданих чи зображеннях торкається базового класу всіх медіаелементів, і її наслідки важко передбачити. Статичні залежності роблять клас майже нетестованим: щоб перевірити `IsParentalAllowed`, треба ініціалізувати глобальні сервіси.
- **Нова функція:** нове правило доступу (наприклад, обмеження за часом доби) доведеться дописувати в клас на 3000 рядків, від якого успадковуються десятки інших.

## 3. Розширення за OCP

### 3.1. Polly: нові стратегії через `ResilienceStrategy` і `AddStrategy`

- [`ResilienceStrategy`](https://github.com/App-vNext/Polly/blob/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec/src/Polly.Core/ResilienceStrategy.cs#L6-L23) — абстрактний клас з єдиним методом `ExecuteCore`.
- [`ResiliencePipelineBuilderExtensions.AddStrategy`](https://github.com/App-vNext/Polly/blob/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec/src/Polly.Core/ResiliencePipelineBuilderExtensions.cs#L69-L119) додає в пайплайн будь-яку стратегію через фабрику.
- Самі вбудовані стратегії підключаються так само, наприклад `AddCircuitBreaker` — це лише виклик `builder.AddStrategy(...)` ([рядок 68](https://github.com/App-vNext/Polly/blob/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec/src/Polly.Core/CircuitBreaker/CircuitBreakerResiliencePipelineBuilderExtensions.cs#L68)).
- Друга точка розширення — делегати в налаштуваннях: `ShouldHandle` і `DelayGenerator` ([рядки 99 і 111](https://github.com/App-vNext/Polly/blob/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec/src/Polly.Core/Retry/RetryStrategyOptions.TResult.cs#L99-L111)). Стратегія лише викликає їх ([рядки 68 і 87–91](https://github.com/App-vNext/Polly/blob/5f9462dc28e42c8ecacde41b208fdd0ed17b34ec/src/Polly.Core/Retry/RetryResilienceStrategy.cs#L68-L91)), тому поведінку змінюють ззовні.
- **Нова функція:** власну стратегію (наприклад, кешування) пишуть як новий клас-нащадок `ResilienceStrategy` і підключають через `AddStrategy`. Код Polly не змінюється.
- **Ризик:** багато точок розширення ускладнюють розуміння порядку виконання (що спрацює першим — Timeout чи Retry).

### 3.2. Jellyfin: провайдери метаданих як плагіни

- Абстракція: [`IRemoteMetadataProvider<TItemType, TLookupInfoType>`](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Controller/Providers/IRemoteMetadataProvider.cs#L21-L31) з методом `GetMetadata`.
- Реалізації: TMDb ([`TmdbEpisodeProvider`, рядок 24](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Providers/Plugins/Tmdb/TV/TmdbEpisodeProvider.cs#L24)), AudioDB, OMDb, MusicBrainz тощо.
- Підключення: `ApplicationHost.FindParts` знаходить усі реалізації через `GetExports<IMetadataProvider>()` і передає їх у `ProviderManager.AddParts` ([рядки 736–743](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs#L736-L743), [ProviderManager, рядки 153–165](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Providers/Manager/ProviderManager.cs#L153-L165)).
- Так само влаштовані заплановані задачі: [`IScheduledTask`](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/MediaBrowser.Model/Tasks/IScheduledTask.cs#L11-L48) + `GetExports<IScheduledTask>()` ([рядок 424](https://github.com/jellyfin/jellyfin/blob/bf14e863d42cf65cf1e950e437e0055c89a26251/Emby.Server.Implementations/ApplicationHost.cs#L424)).
- **Нова функція:** нове джерело метаданих (наприклад, український кінопортал) — це новий клас або окремий плагін, що реалізує інтерфейс. `ProviderManager` і `ApplicationHost` не змінюються.
- **Ризик:** провайдери знаходяться через рефлексію, тому помилку в реалізації видно лише під час запуску, а не під час компіляції.

---

## 4. Пропозиція рефакторингу (найгірший приклад — `BaseItem`)

`BaseItem` гірший за `ApplicationHost`: він базовий для всієї ієрархії медіаелементів, і через статичні сервіси його не можна протестувати ізольовано. Пропоную почати з винесення правил доступу в окремий сервіс:

```csharp
public interface IItemAccessPolicy
{
    bool IsVisible(BaseItem item, User user, bool skipAllowedTagsCheck = false);
    bool IsParentalAllowed(BaseItem item, User user, bool skipAllowedTagsCheck);
    bool CanDelete(BaseItem item, User user);
}

public class ItemAccessPolicy : IItemAccessPolicy
{
    private readonly IServerConfigurationManager _config;
    private readonly ILibraryManager _libraryManager;

    public ItemAccessPolicy(IServerConfigurationManager config, ILibraryManager libraryManager)
    {
        _config = config;
        _libraryManager = libraryManager;
    }

    // логіка з BaseItem (рядки 868–921 і 1919–2095) переноситься сюди
}
```

**Кроки:**
1. Перенести `IsParentalAllowed`, `IsVisible` і `CanDelete` в `ItemAccessPolicy`, а залежності отримувати через конструктор, а не через статичні властивості.
2. Зареєструвати сервіс у DI.
3. Залишити в `BaseItem` тимчасові методи-обгортки з `[Obsolete]`, які викликають політику, щоб не зламати плагіни.
4. Покрити `ItemAccessPolicy` юніт-тестами (тепер це можливо без глобального стану).
5. Тим самим способом винести роботу із зображеннями (`IItemImageService`) і позначки перегляду.

**Результат:** нові правила доступу (наприклад, за часом доби) додаються в окремий сервіс або декоратор `IItemAccessPolicy` без зміни базового класу, тобто рефакторинг одночасно виправляє SRP і відкриває точку розширення за OCP.

---

## 5. Висновки

1. В обох проєктах OCP реалізовано однаково: є абстракція (абстрактний клас або інтерфейс) і механізм реєстрації реалізацій (`AddStrategy` у Polly, `GetExports<T>()` у Jellyfin). Новий функціонал додається новим класом без зміни існуючого коду.
2. Polly як невелика бібліотека з чіткою межею має дуже послідовний SRP: налаштування, виконання і допоміжна математика завжди в окремих класах.
3. Jellyfin виріс із проєкту Emby, і в ядрі залишилися класи, що поєднують багато відповідальностей (`ApplicationHost`, `BaseItem`). При цьому інші частини (заплановані задачі, `Emby.Naming`) написані з дотриманням SRP.
4. Порушення SRP найчастіше видно не за кількістю рядків, а за кількістю різних причин для змін і за статичними залежностями: саме вони роблять клас важким для тестування.
5. Добре застосований OCP і SRP підсилюють один одного: якщо відповідальність винесена в окремий клас за інтерфейсом, її легко замінити або розширити.
6. Рефакторинг великих класів у живому проєкті треба робити поступово, зі збереженням старого API (`[Obsolete]`-обгортки), бо від них залежать плагіни сторонніх розробників.

## Контрольні запитання

1. **Як відрізнити окрему відповідальність від великого обсягу коду?** Рахувати не рядки, а причини для змін: якщо клас змінюватимуть різні люди з різних причин (мережа, безпека, UI), відповідальностей кілька. Великий клас з однією причиною (наприклад, складний парсер) може відповідати SRP.
2. **Ознаки дотримання OCP:** абстракції (інтерфейси, абстрактні класи, делегати) у місцях, де очікуються варіації; реєстрація реалізацій замість `switch` за типом; нова функція додається новим класом, а не правкою існуючого.
3. **Чому дивитися на місця розширення, а не лише на інтерфейси:** інтерфейс сам по собі нічого не гарантує, якщо код, що його використовує, все одно містить `if (provider is TmdbProvider)` чи жорсткий список реалізацій. OCP видно саме там, де система підключає нові реалізації.
4. **Ризики рефакторингу «під патерн» без потреби:** зайві шари абстракції, складніший для розуміння код, більше файлів і класів без реальної користі, а також «розширюваність», якою ніхто не користується, але яку треба підтримувати.
