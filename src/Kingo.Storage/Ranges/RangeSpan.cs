using Kingo.Primitives;

namespace Kingo.Storage.Ranges;

public sealed record Between(Key FromKey, Key ToKey) : KeyRange;
