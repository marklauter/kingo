using LanguageExt;

namespace Kingo.Storage.Sqlite;

internal static class FieldNames<D>
    where D : Document
{
    public static string Columns { get; } =
        string.Join(',', DocumentTypeCache<D>.PropertyNames.Select(n => n.ToLowerInvariant()));

    public static string Values { get; } =
        string.Join(',', DocumentTypeCache<D>.PropertyNames.Select(n => $"@{n}"));
}

