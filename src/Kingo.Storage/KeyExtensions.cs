using Kingo.Facts;
using Kingo.Primitives;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Storage;

internal static class KeyExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Resource resource, Identifier relationship) =>
        $"{resource.AsKey()}#{relationship}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Resource resource) =>
        $"{resource.Namespace}:{resource.Name}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Subject subject) =>
        subject.Id.ToString("N");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this SubjectSet subjectSet) =>
        subjectSet.Resource.AsKey(subjectSet.Relationship);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Either<Subject, SubjectSet> e) =>
        e.Match(
            Left: AsKey,
            Right: AsKey);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Identifier identifier) => Key.From(identifier);
}

