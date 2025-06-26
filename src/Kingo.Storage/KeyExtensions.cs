using Kingo.Facts;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Storage;

internal static class KeyExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsKey(this Resource resource, Relationship relationship) =>
        $"{resource.AsKey()}#{relationship.Name}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsKey(this Resource resource) =>
        $"{resource.Namespace.Name}:{resource.Name}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsKey(this Subject subject) =>
        subject.Id.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsKey(this SubjectSet subjectSet) =>
        subjectSet.Resource.AsKey(subjectSet.Relationship);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsKey(this Either<Subject, SubjectSet> e) =>
        e.Match(
            Left: AsKey,
            Right: AsKey);
}

