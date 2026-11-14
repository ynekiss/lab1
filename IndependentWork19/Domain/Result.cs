namespace IndependentWork19.Domain;

/// <summary>
/// Простий тип Result&lt;T&gt; для ОЧІКУВАНИХ неуспіхів (валідація, "немає в наявності"),
/// коли виняток був би надмірним.
/// </summary>
public readonly record struct Result<T>(bool IsSuccess, T Value, string Error)
{
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);

    public override string ToString() => IsSuccess ? $"Success({Value})" : $"Failure(\"{Error}\")";
}
