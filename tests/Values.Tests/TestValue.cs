using Results;

namespace Values.Tests;

// Reference IValue implementor for exercising ValueParser. A valid TestValue is one or more lowercase ASCII letters.
public readonly record struct TestValue
    : IValue<TestValue, string>
{
    private readonly string value;

    public string Value => value;

    public static TestValue Create(string value) => new(value);

    public static Result<TestValue> Parse(string s) =>
        !string.IsNullOrEmpty(s) && s.All(char.IsAsciiLetterLower)
            ? Result.Success(new TestValue(s))
            : Result.Failure<TestValue>(Error.Validation("test.invalid", $"'{s}' is not one or more lowercase ASCII letters"));

    // The canonical one-line delegation prescribed by the IValue<TSelf, TValue> docs.
    public static bool TryParse(string s, out TestValue parsed) =>
        ValueParser.TryParse<TestValue, string>(s, out parsed);

    private TestValue(string value) => this.value = value;

    public override string ToString() => value;

    public int CompareTo(TestValue other) => string.CompareOrdinal(value, other.value);

    public static bool operator <(TestValue left, TestValue right) => left.CompareTo(right) < 0;

    public static bool operator <=(TestValue left, TestValue right) => left.CompareTo(right) <= 0;

    public static bool operator >(TestValue left, TestValue right) => left.CompareTo(right) > 0;

    public static bool operator >=(TestValue left, TestValue right) => left.CompareTo(right) >= 0;
}
