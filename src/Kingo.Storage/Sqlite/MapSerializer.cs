using Kingo.Storage.Keys;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Kingo.Storage.Sqlite;

// todo: need tests for this
internal static class MapSerializer
{
    public static string Serialize(this Map<Key, object> data) =>
        JsonSerializer.Serialize(data);

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "empty is preferred here")]
    public static Map<Key, object> Deserialize(string data) =>
        JsonSerializer.Deserialize<Dictionary<Key, object>>(data)?.ToMap() ?? Map<Key, object>.Empty;
}
