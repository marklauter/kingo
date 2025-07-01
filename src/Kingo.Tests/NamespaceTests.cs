using Kingo.Specs;
using System.Text.Json;

namespace Kingo.Tests;

public class NamespaceTests
{
    [Fact]
    public async Task ParsesFromJsonAsync()
    {
        var json = await File.ReadAllTextAsync("NamespaceConfiguration.json");
        var nsSpec = JsonSerializer.Deserialize<NamespaceSpec>(json);
        Assert.NotNull(nsSpec);
        // todo: assert on spec content
    }
}
