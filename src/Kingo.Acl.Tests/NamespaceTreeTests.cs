using Kingo.Acl.Namespaces.Tree;

namespace Kingo.Acl.Tests;

public sealed class NamespaceTreeTests
{
    [Fact]
    public async Task ConvertsToTreeAsync()
    {
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");
        Assert.NotNull(tree);
        Assert.True(tree.Relationships.TryGetValue("owner", out var node));
        _ = Assert.IsType<This>(node);

        Assert.True(tree.Relationships.TryGetValue("editor", out node));
        var editor = Assert.IsType<UnionRewrite>(node);
        Assert.Contains(editor!.Children, c => c.GetType() == typeof(This));
        Assert.Contains(editor!.Children, c => c.GetType() == typeof(ComputedSubjectSetRewrite));
        Assert.True((editor!.Children.First(c => c is ComputedSubjectSetRewrite) as ComputedSubjectSetRewrite)!.Relationship.Equals("owner"));

        Assert.True(tree.Relationships.TryGetValue("viewer", out node));
        var viewer = Assert.IsType<UnionRewrite>(node);
        Assert.Contains(viewer!.Children, c => c.GetType() == typeof(This));
        Assert.Contains(viewer!.Children, c => c.GetType() == typeof(ComputedSubjectSetRewrite));
        Assert.True((viewer!.Children.First(c => c is ComputedSubjectSetRewrite) as ComputedSubjectSetRewrite)!.Relationship.Equals("editor"));
    }
}
