# Самостійна робота №10. Інвертований індекс і вимірювання продуктивності (InvertedIndexDemo)

## Класи

| Файл | Призначення |
|---|---|
| `Document.cs` | документ (`Id`, `Title`, `Content`) |
| `TextProcessor.cs` | `Tokenize` (Regex `[\w#+]+` + нижній регістр), `RemoveStopWords`, `Normalize` |
| `InvertedIndex.cs` | `AddDocument`, `SearchSingleWord`, `SearchMultipleWords` (AND, `IntersectWith`), `SearchAnyWord` (OR, `UnionWith`) + завдання з зірочкою: `SearchPhrase`, `SearchRanked` (TF-IDF), `Highlight` |
| `PerformanceTest.cs` | генерація 10/100/1000/10000 документів і порівняння лінійного пошуку з індексом |
| `Program.cs` | демонстрація, бенчмарк та інтерактивний пошук |

**Звіт з таблицею вимірювань і висновками: [REPORT.md](REPORT.md)**

## Запуск

```
cd InvertedIndexDemo
dotnet run -c Release
```

Після демонстрації й бенчмарку програма переходить в інтерактивний режим:

```
Enter search query (or 'exit'): programming language

--- Search Results (AND) ---
[1] Introduction to C#
  C# is a modern object-oriented [programming] [language] developed by Microsoft....
[2] Java Basics
  Java is a popular [programming] [language] known for its portability and performance....
[3] Python Overview
  Python is a high-level [programming] [language] with simple syntax....

Found 3 documents

Enter search query (or 'exit'): c# async

--- Search Results (AND) ---
[4] C# Advanced Features
  [C#] supports LINQ, [async]/await, and many modern programming paradigms....

Found 1 documents

Enter search query (or 'exit'): exit
```

Знайдені слова підсвічуються квадратними дужками (завдання з зірочкою №3).

Для запуску без інтерактивного режиму: `dotnet run -c Release -- --no-interactive`.

## Приклад виводу (неінтерактивна частина)

```
Loaded 5 documents
Index contains 37 unique words

=== Демонстрація методів пошуку ===
SearchSingleWord("python"): 3, 5
SearchMultipleWords("programming", "language")  [AND]: 1, 2, 3
SearchMultipleWords("c#", "async")  [AND]: 4
SearchAnyWord("java", "python")  [OR]: 2, 3, 5
SearchPhrase("programming language")  (*): 1, 2, 3
SearchPhrase("language programming")  (*): нічого не знайдено
SearchRanked("c#", "modern")  (*) TF-IDF:
  [1] Introduction to C#  score = 3,427
  [4] C# Advanced Features  score = 3,427
Стоп-слова: c# language web

=== Вимірювання продуктивності (середнє з 50 пошуків) ===
| Документів | Слово       | Лінійний, мкс | Індекс, мкс | Прискорення | Побудова індексу, мс | Знайдено (лін/інд) |
|-----------:|-------------|--------------:|------------:|------------:|---------------------:|:------------------:|
|         10 | programming |          71,0 |         2,1 |         34x |                 0,23 |       10 / 10      |
|         10 | microsoft   |         133,0 |         1,5 |         91x |                 0,21 |        2 / 2       |
|        100 | programming |         642,5 |        10,1 |         63x |                 1,79 |      100 / 100     |
|        100 | microsoft   |        1038,5 |         2,1 |        498x |                 1,34 |       20 / 20      |
|       1000 | programming |        4368,9 |       214,4 |         20x |                 7,37 |     1000 / 1000    |
|       1000 | microsoft   |        3142,0 |        82,0 |         38x |                11,52 |      200 / 200     |
|      10000 | programming |       12215,5 |       246,4 |         50x |                59,59 |    10000 / 10000   |
|      10000 | microsoft   |       25356,2 |        34,0 |        745x |                68,42 |     2000 / 2000    |
Кількість знайдених документів збігається в усіх тестах: так
```
