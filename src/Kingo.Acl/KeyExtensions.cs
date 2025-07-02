using Kingo.Storage.Keys;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Acl;

public static class KeyExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsHashKey<NS>(this Resource resource, Relationship relationship) =>
        $"{Namespace<NS>.Value}/{resource.AsString()}#{relationship}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsHashKey<NS>(this SubjectSet subjectSet) =>
        subjectSet.Resource.AsHashKey<NS>(subjectSet.Relationship);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsRangeKey(this Resource resource, Relationship relationship) =>
        $"{resource.AsString()}#{relationship}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AsString(this Resource resource) =>
        $"{resource.Namespace}:{resource.Name}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsRangeKey(this Relationship relationship) =>
        relationship.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsRangeKey(this Either<Subject, SubjectSet> e) =>
        e.Match(
            Left: AsRangeKey,
            Right: AsRangeKey);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsRangeKey(this Subject subject) =>
        subject.Id.ToString("N");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsRangeKey(this SubjectSet subjectSet) =>
        subjectSet.Resource.AsRangeKey(subjectSet.Relationship);
}

internal static class Namespace<NS>
{
    public static string Value { get; } = typeof(NS).Name;
}
