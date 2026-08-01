namespace Kingo.Facts.Tests;

public sealed class SubjectSetTests
{
    private static SubjectSet Of(string namespacePath, string resourceId, string relation) =>
        new(
            new Resource(NamespacePath.Unchecked(namespacePath), ResourceId.Unchecked(resourceId)),
            RelationName.Unchecked(relation));

    [Fact]
    public void Construction_HoldsResourceAndRelation()
    {
        var set = Of("io/doc", "readme", "viewer");

        Assert.Equal(new Resource(NamespacePath.Unchecked("io/doc"), ResourceId.Unchecked("readme")), set.Resource);
        Assert.Equal(RelationName.Unchecked("viewer"), set.Relation);
    }

    [Fact]
    public void Equality_EqualParts_ProduceEqualValues() => Assert.Equal(Of("io/doc", "readme", "viewer"), Of("io/doc", "readme", "viewer"));

    [Fact]
    public void Equality_DifferentRelation_ProducesUnequalValues() => Assert.NotEqual(Of("io/doc", "readme", "viewer"), Of("io/doc", "readme", "editor"));
}
