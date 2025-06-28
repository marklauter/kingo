using Kingo.Specifications;
using System.Text.Json;

namespace Kingo.Tests;

public class NamespaceConfigurationTests
{
    [Fact]
    public async Task ParsesFromJson()
    {
        var json = await File.ReadAllTextAsync("NamespaceConfiguration.json");
        var nsConfig = JsonSerializer.Deserialize<NamespaceSpec>(json);
        Assert.NotNull(nsConfig);
    }
}
