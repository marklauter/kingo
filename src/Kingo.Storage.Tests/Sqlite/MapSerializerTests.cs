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
        var map = Map<Key, object>();
        var json = map.Serialize();
        json.Should().Be("{}");
    }

    [Fact]
    public void Deserialize_ReturnsEmptyMap_WhenJsonIsEmpty()
    {
        const string json = "{}";
        var map = MapSerializer.Deserialize(json);
        map.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_ReturnsCorrectJson_WhenMapHasData()
    {
        var map = Map<Key, object>()
            .Add(Key.From("key1"), "value1")
            .Add(Key.From("key2"), 123)
            .Add(Key.From("key3"), true);

        var json = map.Serialize();
        json.Should().Be("{\"key1\":\"value1\",\"key2\":123,\"key3\":true}");
    }

    [Fact]
    public void Deserialize_ReturnsCorrectMap_WhenJsonHasData()
    {
        const string json = "{\"key1\":\"value1\",\"key2\":123,\"key3\":true}";
        var map = MapSerializer.Deserialize(json);

        map.Count.Should().Be(3);
        map[Key.From("key1")].ToString().Should().Be("value1");
        map[Key.From("key2")].ToString().Should().Be("123");
        map[Key.From("key3")].ToString().Should().Be("True");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesMap()
    {
        var originalMap = Map<Key, object>()
            .Add(Key.From("a"), "string")
            .Add(Key.From("b"), 42)
            .Add(Key.From("c"), false)
            .Add(Key.From("d"), 3.14);

        var json = originalMap.Serialize();
        var deserializedMap = MapSerializer.Deserialize(json);

        deserializedMap.Count.Should().Be(originalMap.Count);
        foreach (var (key, value) in originalMap)
        {
            deserializedMap.ContainsKey(key).Should().BeTrue();
            // Values are deserialized as JsonElement, so we compare their string representations
            deserializedMap[key].ToString().Should().Be(value.ToString());
        }
    }
}
