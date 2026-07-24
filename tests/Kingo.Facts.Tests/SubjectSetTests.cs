namespace Kingo.Facts.Tests;

public sealed class SubjectSetTests
{
    private static SubjectSet Of(string namespacePath, string resourceId, string relationship) =>
        new(
            new Resource(NamespacePath.Unchecked(namespacePath), ResourceId.Unchecked(resourceId)),
            RelationshipName.Unchecked(relationship));

    [Fact]
    public void Construction_HoldsResourceAndRelationship()
    {
        var set = Of("io/doc", "readme", "viewer");

        Assert.Equal(new Resource(NamespacePath.Unchecked("io/doc"), ResourceId.Unchecked("readme")), set.Resource);
        Assert.Equal(RelationshipName.Unchecked("viewer"), set.Relationship);
    }

    [Fact]
    public void Equality_EqualParts_ProduceEqualValues() => Assert.Equal(Of("io/doc", "readme", "viewer"), Of("io/doc", "readme", "viewer"));

    [Fact]
    public void Equality_DifferentRelationship_ProducesUnequalValues() => Assert.NotEqual(Of("io/doc", "readme", "viewer"), Of("io/doc", "readme", "editor"));
}
