namespace Kingo.Storage.Keys;

public sealed record Between(Key FromKey, Key ToKey) : KeyRange;
