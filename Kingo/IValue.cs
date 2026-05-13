namespace Kingo;

public interface IValue<TSelf, TValue>
    where TSelf : IValue<TSelf, TValue>
{
    TValue Value { get; }
    static abstract TSelf Create(TValue value);
    static abstract Result<TSelf> Parse(string s);
    static abstract bool TryParse(string s, out TSelf parsed);
}
