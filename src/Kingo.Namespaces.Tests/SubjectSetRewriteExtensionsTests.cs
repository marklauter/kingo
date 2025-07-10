//using Kingo.Namespaces.Serializable;
//using Kingo.Storage.Keys;

//namespace Kingo.Namespaces.Tests;

//public sealed class SubjectSetRewriteExtensionsTests
//{
//    [Fact]
//    public void TransformRewrite_This_ReturnsDefaultThis()
//    {
//        var serializableThis = new Serializable.This();

//        var result = serializableThis.TransformRewrite();

//        _ = Assert.IsType<This>(result);
//        Assert.Equal(This.Default, result);
//    }

//    [Fact]
//    public void TransformRewrite_ComputedSubjectSetRewrite_ReturnsComputedSubjectSetRewrite()
//    {
//        var relationship = Relationship.From("owner");
//        var serializable = new Serializable.ComputedSubjectSetRewrite(relationship);

//        var result = serializable.TransformRewrite();

//        var computed = Assert.IsType<ComputedSubjectSetRewrite>(result);
//        Assert.Equal(relationship, computed.Relationship);
//    }

//    [Fact]
//    public void TransformRewrite_UnionRewrite_ReturnsUnionRewrite()
//    {
//        var children = new List<Serializable.SubjectSetRewrite>
//        {
//            new Serializable.This(),
//            new Serializable.ComputedSubjectSetRewrite(Relationship.From("owner"))
//        };
//        var serializable = new Serializable.UnionRewrite(children);

//        var result = serializable.TransformRewrite();

//        var union = Assert.IsType<UnionRewrite>(result);
//        Assert.Equal(2, union.Children.Count);
//        _ = Assert.IsType<This>(union.Children[0]);
//        _ = Assert.IsType<ComputedSubjectSetRewrite>(union.Children[1]);
//    }

//    [Fact]
//    public void TransformRewrite_IntersectionRewrite_ReturnsIntersectionRewrite()
//    {
//        var children = new List<Serializable.SubjectSetRewrite>
//        {
//            new Serializable.This(),
//            new Serializable.ComputedSubjectSetRewrite(Relationship.From("editor"))
//        };
//        var serializable = new Serializable.IntersectionRewrite(children);

//        var result = serializable.TransformRewrite();

//        var intersection = Assert.IsType<IntersectionRewrite>(result);
//        Assert.Equal(2, intersection.Children.Count);
//        _ = Assert.IsType<This>(intersection.Children[0]);
//        _ = Assert.IsType<ComputedSubjectSetRewrite>(intersection.Children[1]);
//    }

//    [Fact]
//    public void TransformRewrite_ExclusionRewrite_ReturnsExclusionRewrite()
//    {
//        var include = new Serializable.ComputedSubjectSetRewrite(Relationship.From("owner"));
//        var exclude = new Serializable.This();
//        var serializable = new Serializable.ExclusionRewrite(include, exclude);

//        var result = serializable.TransformRewrite();

//        var exclusion = Assert.IsType<ExclusionRewrite>(result);
//        _ = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Include);
//        _ = Assert.IsType<This>(exclusion.Exclude);
//    }

//    [Fact]
//    public void TransformRewrite_TupleToSubjectSetRewrite_ReturnsTupleToSubjectSetRewrite()
//    {
//        var tuplesetRelation = Relationship.From("parent");
//        var computedRelation = Relationship.From("viewer");
//        var serializable = new Serializable.TupleToSubjectSetRewrite(tuplesetRelation, computedRelation);

//        var result = serializable.TransformRewrite();

//        var tuple = Assert.IsType<TupleToSubjectSetRewrite>(result);
//        Assert.Equal(tuplesetRelation, tuple.TuplesetRelation);
//        Assert.Equal(computedRelation, tuple.ComputedSubjectSetRelation);
//    }

//    [Fact]
//    public void TransformRewrite_UnknownType_ThrowsNotSupportedException()
//    {
//        var unknown = new UnknownSubjectSetRewrite();

//        var exception = Assert.Throws<NotSupportedException>(unknown.TransformRewrite);

//        Assert.NotNull(exception);
//    }

//    [Fact]
//    public void TransformRewrite_NestedUnionRewrite_TransformsRecursively()
//    {
//        var nestedUnion = new Serializable.UnionRewrite(new List<Serializable.SubjectSetRewrite>
//        {
//            new Serializable.This(),
//            new Serializable.ComputedSubjectSetRewrite(Relationship.From("nested"))
//        });

//        var outerUnion = new Serializable.UnionRewrite(new List<Serializable.SubjectSetRewrite>
//        {
//            nestedUnion,
//            new Serializable.ComputedSubjectSetRewrite(Relationship.From("outer"))
//        });

//        var result = outerUnion.TransformRewrite();

//        var union = Assert.IsType<UnionRewrite>(result);
//        Assert.Equal(2, union.Children.Count);
//        _ = Assert.IsType<UnionRewrite>(union.Children[0]);
//        _ = Assert.IsType<ComputedSubjectSetRewrite>(union.Children[1]);
//    }

//    [Fact]
//    public void TransformRewrite_NestedIntersectionRewrite_TransformsRecursively()
//    {
//        var nestedIntersection = new Serializable.IntersectionRewrite(new List<Serializable.SubjectSetRewrite>
//        {
//            new Serializable.This(),
//            new Serializable.ComputedSubjectSetRewrite(Relationship.From("nested"))
//        });

//        var outerIntersection = new Serializable.IntersectionRewrite(new List<Serializable.SubjectSetRewrite>
//        {
//            nestedIntersection,
//            new Serializable.ComputedSubjectSetRewrite(Relationship.From("outer"))
//        });

//        var result = outerIntersection.TransformRewrite();

//        var intersection = Assert.IsType<IntersectionRewrite>(result);
//        Assert.Equal(2, intersection.Children.Count);
//        _ = Assert.IsType<IntersectionRewrite>(intersection.Children[0]);
//        _ = Assert.IsType<ComputedSubjectSetRewrite>(intersection.Children[1]);
//    }

//    [Fact]
//    public void TransformRewrite_NestedExclusionRewrite_TransformsRecursively()
//    {
//        var nestedInclude = new Serializable.ComputedSubjectSetRewrite(Relationship.From("nestedInclude"));
//        var nestedExclude = new Serializable.This();
//        var nestedExclusion = new Serializable.ExclusionRewrite(nestedInclude, nestedExclude);

//        var outerInclude = new Serializable.ComputedSubjectSetRewrite(Relationship.From("outerInclude"));
//        var outerExclusion = new Serializable.ExclusionRewrite(outerInclude, nestedExclusion);

//        var result = outerExclusion.TransformRewrite();

//        var exclusion = Assert.IsType<ExclusionRewrite>(result);
//        _ = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Include);
//        _ = Assert.IsType<ExclusionRewrite>(exclusion.Exclude);
//    }

//    [Fact]
//    public async Task TransformRewrite_NamespaceSpec_ReturnsCorrectDocuments()
//    {
//        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");

//        var result = spec.TransformRewrite();

//        Assert.Equal(4, result.Count);

//        var documents = result.ToArray();

//        // Check owner relationship
//        var ownerDoc = documents[0];
//        Assert.Equal(Key.From("Namespace/doc"), ownerDoc.HashKey.ToString());
//        Assert.Equal(Key.From("owner"), ownerDoc.RangeKey.ToString());
//        _ = ownerDoc[Key.From("ssr")].Map(Assert.IsType<This>).IfNone(() => Assert.Fail("fail"));

//        // Check editor relationship
//        var editorDoc = documents[1];
//        Assert.Equal(Key.From("Namespace/doc"), editorDoc.HashKey.ToString());
//        Assert.Equal(Key.From("editor"), editorDoc.RangeKey.ToString());
//        _ = editorDoc[Key.From("ssr")].Map(Assert.IsType<UnionRewrite>).IfNone(() => Assert.Fail("fail"));

//        // Check viewer relationship
//        var viewerDoc = documents[2];
//        Assert.Equal(Key.From("Namespace/doc"), viewerDoc.HashKey.ToString());
//        Assert.Equal(Key.From("viewer"), viewerDoc.RangeKey.ToString());
//        _ = viewerDoc[Key.From("ssr")].Map(Assert.IsType<UnionRewrite>).IfNone(() => Assert.Fail("fail"));

//        var auditorDoc = documents[3];
//        Assert.Equal(Key.From("Namespace/doc"), auditorDoc.HashKey.ToString());
//        Assert.Equal(Key.From("auditor"), auditorDoc.RangeKey.ToString());
//        _ = auditorDoc[Key.From("ssr")].Map(Assert.IsType<IntersectionRewrite>).IfNone(() => Assert.Fail("fail"));
//    }

//    [Fact]
//    public async Task TransformRewrite_NamespaceSpec_CreatesCorrectHashKeys()
//    {
//        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");

//        var result = spec.TransformRewrite();

//        var documents = result.ToArray();
//        var expectedHashKey = Key.From("Namespace/doc");

//        Assert.All(documents, doc => Assert.Equal(expectedHashKey, doc.HashKey));
//    }

//    [Fact]
//    public async Task TransformRewrite_NamespaceSpec_CreatesCorrectRangeKeys()
//    {
//        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");

//        var result = spec.TransformRewrite();

//        var documents = result.ToArray();
//        var expectedRangeKeys = new[] { "owner", "editor", "viewer", "auditor" };

//        for (var i = 0; i < documents.Length; i++)
//        {
//            Assert.Equal(expectedRangeKeys[i], documents[i].RangeKey.ToString());
//        }
//    }

//    [Fact]
//    public async Task TransformRewrite_NamespaceSpec_TransformsComplexViewer()
//    {
//        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");

//        var result = spec.TransformRewrite();

//        var viewerDoc = result.ToArray()[2];
//        var viewerRewrite = viewerDoc[Key.From("ssr")].Map(Assert.IsType<UnionRewrite>);
//        _ = viewerRewrite.IfNone(() => Assert.Fail("doc not found"));
//        _ = viewerRewrite.IfSome(ur =>
//        {
//            Assert.Equal(3, ur.Children.Count);
//            _ = Assert.IsType<This>(ur.Children[0]);

//            var editorComputed = Assert.IsType<ComputedSubjectSetRewrite>(ur.Children[1]);
//            Assert.Equal("editor", editorComputed.Relationship.ToString());

//            var tupleToSubject = Assert.IsType<TupleToSubjectSetRewrite>(ur.Children[2]);
//            Assert.Equal("parent", tupleToSubject.TuplesetRelation.ToString());
//            Assert.Equal("viewer", tupleToSubject.ComputedSubjectSetRelation.ToString());
//        });
//    }

//    [Fact]
//    public void TransformRewrite_EmptyNamespaceSpec_ReturnsEmptySequence()
//    {
//        var spec = new NamespaceSpec(Namespace.From("empty"), []);

//        var result = spec.TransformRewrite();

//        Assert.True(result.IsEmpty);
//    }

//    [Fact]
//    public void TransformRewrite_SingleRelationship_ReturnsSingleDocument()
//    {
//        var relationship = new RelationshipSpec(Relationship.From("test"), new Serializable.This());
//        var spec = new NamespaceSpec(Namespace.From("test"), [relationship]);

//        var result = spec.TransformRewrite();

//        Assert.Equal(1, result.Count);

//        _ = result.Head.Match(
//            Some: doc =>
//            {
//                Assert.Equal(Key.From("Namespace/test"), doc.HashKey.ToString());
//                Assert.Equal(Key.From("test"), doc.RangeKey.ToString());
//                _ = doc[Key.From("ssr")].Map(Assert.IsType<This>).IfNone(() => Assert.Fail("fail"));
//            },
//            None: () => Assert.Fail("Expected a document but got None."));
//    }

//    // Helper class for testing unsupported types
//    private sealed record UnknownSubjectSetRewrite : Serializable.SubjectSetRewrite;
//}
