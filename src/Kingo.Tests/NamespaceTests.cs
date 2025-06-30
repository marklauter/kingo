using Kingo.Configuration.Spec;
using Kingo.Configuration.Tree;
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

    [Fact]
    public async Task ConvertsToTreeAsync()
    {
        var json = await File.ReadAllTextAsync("NamespaceConfiguration.json");
        var nsSpec = JsonSerializer.Deserialize<NamespaceSpec>(json)!;
        var tree = NamespaceTree.FromSpec(nsSpec);
        Assert.NotNull(tree);
        Assert.True(tree.Relationships.TryGetValue("owner", out var owner));
        Assert.Equal(ThisNode.This, owner);

        Assert.True(tree.Relationships.TryGetValue("editor", out var editorSet));
        Assert.True(editorSet is OperationNode);
        var editor = editorSet as OperationNode;
        Assert.Equal(SetOperation.Union, editor!.Operation);
        Assert.Contains(editor!.Children, c => c.GetType() == typeof(ThisNode));
        Assert.Contains(editor!.Children, c => c.GetType() == typeof(ComputedSubjectSetNode));
        Assert.True((editor!.Children.First(c => c is ComputedSubjectSetNode) as ComputedSubjectSetNode)!.Relationship.Equals("owner"));

        Assert.True(tree.Relationships.TryGetValue("viewer", out var viwerSet));
        Assert.True(viwerSet is OperationNode);
        var viewer = viwerSet as OperationNode;
        Assert.Equal(SetOperation.Union, viewer!.Operation);
        Assert.Contains(viewer!.Children, c => c.GetType() == typeof(ThisNode));
        Assert.Contains(viewer!.Children, c => c.GetType() == typeof(ComputedSubjectSetNode));
        Assert.True((viewer!.Children.First(c => c is ComputedSubjectSetNode) as ComputedSubjectSetNode)!.Relationship.Equals("editor"));

        Assert.Contains(viewer!.Children, c => c.GetType() == typeof(TupleToSubjectSetNode));
        var tplSet = viewer!.Children.First(c => c is TupleToSubjectSetNode) as TupleToSubjectSetNode;
        Assert.True(tplSet!.Name.Equals("parent"));
        Assert.True(tplSet!.Child is ComputedSubjectSetNode);
        Assert.True(tplSet!.Child is ComputedSubjectSetNode node && node.Relationship.Equals("viewer"));
    }
}
