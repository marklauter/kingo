using Kingo.Storage.Keys;
using System.Runtime.CompilerServices;

namespace Kingo.Acl;

public static class KeyExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this SubjectSet subjectSet) =>
        subjectSet.Resource.AsKey(subjectSet.Relationship);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Resource resource, Relationship relationship) =>
        $"{resource.AsString()}#{relationship}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AsString(this Resource resource) =>
        $"{resource.Namespace}:{resource.Name}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key AsKey(this Subject subject) =>
        subject.Id.ToString("N");
}
