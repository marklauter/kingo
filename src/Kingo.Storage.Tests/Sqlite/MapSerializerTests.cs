using FluentAssertions;
using Kingo.Storage.Keys;
using Kingo.Storage.Sqlite;
using LanguageExt;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class MapSerializerTests
{
    [Fact]
    public void Serialize_ReturnsEmptyJson_WhenMapIsEmpty()
    {
        var map = Map<Key, object>.Empty;
        var json = map.Serialize();
        _ = json.Should().Be("{}");
    }

    [Fact]
    public void Deserialize_ReturnsEmptyMap_WhenJsonIsEmpty()
    {
        const string json = "{}";
        var map = MapSerializer.Deserialize(json);
        _ = map.Should().Be(Map<Key, object>.Empty);
    }

    [Fact]
    public void Serialize_ReturnsCorrectJson_WhenMapHasData()
    {
        var map = Map<Key, object>.Empty
            .Add(Key.From("key1"), "value1")
            .Add(Key.From("key2"), 123)
            .Add(Key.From("key3"), true);

        var json = map.Serialize();
        _ = json.Should().Be("{\"key1\":\"value1\",\"key2\":123,\"key3\":true}");
    }

    [Fact]
    public void Deserialize_ReturnsCorrectMap_WhenJsonHasData()
    {
        const string json = "{\"key1\":\"value1\",\"key2\":123,\"key3\":true}";
        var map = MapSerializer.Deserialize(json);

        _ = map.Count.Should().Be(3);
        _ = map[Key.From("key1")].ToString().Should().Be("value1");
        _ = map[Key.From("key2")].ToString().Should().Be("123");
        _ = map[Key.From("key3")].ToString().Should().Be("True");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesMap()
    {
        var originalMap = Map<Key, object>.Empty
            .Add(Key.From("a"), "string")
            .Add(Key.From("b"), 42)
            .Add(Key.From("c"), false)
            .Add(Key.From("d"), 3.14);

        var json = originalMap.Serialize();
        var deserializedMap = MapSerializer.Deserialize(json);

        _ = deserializedMap.Count.Should().Be(originalMap.Count);
        foreach (var (key, value) in originalMap)
        {
            _ = deserializedMap.ContainsKey(key).Should().BeTrue();
            // Values are deserialized as JsonElement, so we compare their string representations
            _ = deserializedMap[key].ToString().Should().Be(value.ToString());
        }
    }
}
