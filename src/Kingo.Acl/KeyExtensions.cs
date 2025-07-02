using Kingo.Facts;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Acl;

public static class KeyExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Resource resource, Relationship relationship) =>
        $"{resource.AsString()}#{relationship}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Resource resource) =>
        resource.AsString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AsString(this Resource resource) =>
        $"{resource.Namespace}:{resource.Name}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Either<Subject, SubjectSet> e) =>
        e.Match(
            Left: AsKey,
            Right: AsKey);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Subject subject) =>
        subject.Id.ToString("N");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this SubjectSet subjectSet) =>
        subjectSet.Resource.AsKey(subjectSet.Relationship);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Identifier identifier) => Key.From(identifier);
}
