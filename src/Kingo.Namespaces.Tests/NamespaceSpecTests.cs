using Kingo.Namespaces.Serializable;

namespace Kingo.Namespaces.Tests;

public class NamespaceSpecTests
{
    [Theory]
    [InlineData("Data/Namespace.Doc.json", "doc")]
    [InlineData("Data/Namespace.Folder.json", "folder")]
    public async Task ParsesFromJsonAsync(string path, string ns)
    {
        var nsSpec = await NamespaceSpec.FromFileAsync(path);
        Assert.NotNull(nsSpec);

        Assert.Equal(ns, nsSpec.Name);
        Assert.Equal(3, nsSpec.Relationships.Count);

        var owner = nsSpec.Relationships[0];
        Assert.Equal("owner", owner.Name);
        _ = Assert.IsType<Serializable.This>(owner.SubjectSetRewrite);

        var editor = nsSpec.Relationships[1];
        Assert.Equal("editor", editor.Name);
        var editorRewrite = Assert.IsType<Serializable.UnionRewrite>(editor.SubjectSetRewrite);
        Assert.Equal(2, editorRewrite.Children.Count);
        _ = Assert.IsType<Serializable.This>(editorRewrite.Children[0]);
        var editorOwner = Assert.IsType<Serializable.ComputedSubjectSetRewrite>(editorRewrite.Children[1]);
        Assert.Equal("owner", editorOwner.Relationship);

        var viewer = nsSpec.Relationships[2];
        Assert.Equal("viewer", viewer.Name);
        var viewerRewrite = Assert.IsType<Serializable.UnionRewrite>(viewer.SubjectSetRewrite);
        Assert.Equal(3, viewerRewrite.Children.Count);
        _ = Assert.IsType<Serializable.This>(viewerRewrite.Children[0]);
        var viewerEditor = Assert.IsType<Serializable.ComputedSubjectSetRewrite>(viewerRewrite.Children[1]);
        Assert.Equal("editor", viewerEditor.Relationship);

        var tupleToUserset = Assert.IsType<Serializable.TupleToSubjectSetRewrite>(viewerRewrite.Children[2]);
        Assert.Equal("parent", tupleToUserset.TuplesetRelation);
        Assert.Equal("viewer", tupleToUserset.ComputedSubjectSetRelation);
    }
}
