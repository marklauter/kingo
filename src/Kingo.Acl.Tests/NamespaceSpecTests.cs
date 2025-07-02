using Kingo.Acl.Namespaces.Spec;

namespace Kingo.Acl.Tests;

public class NamespaceSpecTests
{
    [Fact]
    public async Task ParsesFromJsonAsync()
    {
        var nsSpec = await NamespaceSpec.FromFileAsync("NamespaceConfiguration.json");
        Assert.NotNull(nsSpec);

        Assert.Equal("doc", nsSpec.Name);
        Assert.Equal(3, nsSpec.Relationships.Count);

        var owner = nsSpec.Relationships[0];
        Assert.Equal("owner", owner.Name);
        _ = Assert.IsType<This>(owner.SubjectSetRewrite);

        var editor = nsSpec.Relationships[1];
        Assert.Equal("editor", editor.Name);
        var editorRewrite = Assert.IsType<UnionRewrite>(editor.SubjectSetRewrite);
        Assert.Equal(2, editorRewrite.Children.Count);
        _ = Assert.IsType<This>(editorRewrite.Children[0]);
        var editorOwner = Assert.IsType<ComputedSubjectSetRewrite>(editorRewrite.Children[1]);
        Assert.Equal("owner", editorOwner.Relationship);

        var viewer = nsSpec.Relationships[2];
        Assert.Equal("viewer", viewer.Name);
        var viewerRewrite = Assert.IsType<UnionRewrite>(viewer.SubjectSetRewrite);
        Assert.Equal(3, viewerRewrite.Children.Count);
        _ = Assert.IsType<This>(viewerRewrite.Children[0]);
        var viewerEditor = Assert.IsType<ComputedSubjectSetRewrite>(viewerRewrite.Children[1]);
        Assert.Equal("editor", viewerEditor.Relationship);

        var tupleToUserset = Assert.IsType<TupleToSubjectSetRewrite>(viewerRewrite.Children[2]);
        Assert.Equal("parent", tupleToUserset.TuplesetRelation);
        Assert.Equal("viewer", tupleToUserset.ComputedSubjectSetRelation);
    }
}
