# Самостійна робота №8. Dictionary<TKey, TValue> та HashSet<T> у бібліотеках .NET

- `FileMetadataCache.cs`, сценарій 1: кеш метаданих файлів (`Dictionary` + `StringComparer.OrdinalIgnoreCase`);
- `TagManager.cs`, сценарій 2: теги статей (`Dictionary<string, HashSet<string>>`, `UnionWith` / `IntersectWith` / `ExceptWith`);
- `SearchIndex.cs`, сценарій 3: інвертований індекс (`Dictionary<string, HashSet<int>>`);
- `Program.cs`: демонстрація + приклади custom comparer.

**Звіт (аналіз Newtonsoft.Json, порівняльна таблиця, висновки, відповіді на контрольні запитання): [REPORT.md](REPORT.md)**

## Запуск

```
cd IndependentWork8
dotnet run
```

## Приклад виводу програми

```
=== Сценарій 1. FileMetadataCache (Dictionary) ===
У кеші файлів: 3
Get("c:\docs\REPORT.DOCX") -> Report.docx (48 213 B, змінено 20.10.2026 14:30, hash a1b2c3)
Contains(photo.png) = True
Файл notes.txt змінився? True
Remove(photo.png) = True, тепер у кеші: 2
Get(photo.png) -> null (немає в кеші)

=== Сценарій 2. TagManager (HashSet) ===
Теги post-1: {collections, csharp, dotnet}
Теги post-2: {collections, csharp, linq}
Усі унікальні теги (UnionWith): {beginners, collections, csharp, dotnet, linq, python}
Спільні теги post-1 і post-2 (IntersectWith): {collections, csharp}
Є в post-1, але немає в post-2 (ExceptWith): {dotnet}
Спільні теги post-1 і post-3: (порожньо)
Після RemoveTag(post-1, dotnet): {collections, csharp}

=== Сценарій 3. SearchIndex (Dictionary + HashSet) ===
Унікальних слів в індексі: 11
Search("c#") -> [1, 3]
Search("programming") -> [1, 2]
Search("JAVA") -> [2, 3]
SearchMultiple("c#", "java") -> [3]
SearchMultiple("programming", "language") -> [1, 2]
Search("rust") -> [] (нічого не знайдено)

=== Бонус. Custom comparer ===
caseInsensitive["HELLO"] = 1
Add(John, 25) = True, Add(John, 30) = False, Count = 1
```
