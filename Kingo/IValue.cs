using System.Numerics;

namespace Kingo;

public interface IValue<TSelf, TValue>
    : IComparable<TSelf>
    , IEquatable<TSelf>
    , IComparisonOperators<TSelf, TSelf, bool>
    where TSelf : IValue<TSelf, TValue>
{
    TValue Value { get; }
    static abstract TSelf Create(TValue value);
    static abstract Result<TSelf> Parse(string s);
    // Default implementation — delegates to TSelf.Parse. Implementors may override for hot paths.
    static virtual bool TryParse(string s, out TSelf parsed)
    {
        if (TSelf.Parse(s) is Success<TSelf> success)
        {
            parsed = success.Value;
            return true;
        }
        parsed = default!;
        return false;
    }
}
