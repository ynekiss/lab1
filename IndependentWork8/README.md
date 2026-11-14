# Самостійна робота №8. Dictionary<TKey, TValue> та HashSet<T> у бібліотеках .NET

**Мета:** Дослідити реальні приклади використання узагальнених колекцій у популярних .NET-бібліотеках і застосувати їх на практиці.

**Хід роботи:**
1. Дослідив вихідний код Newtonsoft.Json і розібрав приклади використання `Dictionary` (`BidirectionalDictionary` для `$id`/`$ref`), `HashSet` (злиття масивів JSON без дублікатів) і обох колекцій разом. Для кожного прикладу описав контекст, причину вибору колекції, складність операцій і custom comparer.
2. Створив консольний проєкт `IndependentWork8` з трьома класами:
   - `FileMetadataCache.cs` — кеш метаданих файлів на `Dictionary` з `StringComparer.OrdinalIgnoreCase`;
   - `TagManager.cs` — теги статей на `Dictionary<string, HashSet<string>>` з `UnionWith` / `IntersectWith` / `ExceptWith`;
   - `SearchIndex.cs` — простий інвертований індекс на `Dictionary<string, HashSet<int>>`.
3. У `Program.cs` продемонстрував роботу всіх класів і приклади custom comparer.
4. Склав порівняльну таблицю складності операцій `List` / `Dictionary` / `HashSet`, описав висновки та підводні камені.

**Звіт (аналіз Newtonsoft.Json, порівняльна таблиця, висновки, відповіді на контрольні запитання): [REPORT.md](REPORT.md)**

**Висновок:** `Dictionary` варто обирати, коли дані шукають за ключем, а `HashSet` — коли важлива лише унікальність і потрібні операції над множинами. В обох випадках треба правильно визначити рівність ключів (comparer).

**Запуск:**
```
cd IndependentWork8
dotnet run
```
