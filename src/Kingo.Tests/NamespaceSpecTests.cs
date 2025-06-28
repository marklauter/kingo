using Kingo.Specifications;
using System.Text.Json;

namespace Kingo.Tests;

public class NamespaceSpecTests
{
    [Fact]
    public async Task ParsesFromJsonAsync()
    {
        var json = await File.ReadAllTextAsync("NamespaceConfiguration.json");
        var nsSpec = JsonSerializer.Deserialize<NamespaceSpec>(json);
        Assert.NotNull(nsSpec);
        // todo: assert on spec content
    }

    [Fact]
    public async Task ConvertsToTreeAsync()
    {
        var json = await File.ReadAllTextAsync("NamespaceConfiguration.json");
        var nsSpec = JsonSerializer.Deserialize<NamespaceSpec>(json);
        Assert.NotNull(nsSpec);
        Assert.NotNull(NamespaceTree.FromSpec(nsSpec));
        // todo: assert on tree content
    }
}
