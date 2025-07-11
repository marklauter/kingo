namespace Kingo.Tests;

public sealed class SubjectSetTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        var resource = new Resource(PolicyName.From("ns"), Identifier.From("id"));
        var relationship = RelationName.From("rel");

        var subjectSet = new SubjectSet(resource, relationship);

        Assert.Equal(resource, subjectSet.Resource);
        Assert.Equal(relationship, subjectSet.Relationship);
    }

    [Fact]
    public void Equality_ReturnsTrue_ForEqualInstances()
    {
        var resource = new Resource(PolicyName.From("ns"), Identifier.From("id"));
        var relationship = RelationName.From("rel");

        var subjectSet1 = new SubjectSet(resource, relationship);
        var subjectSet2 = new SubjectSet(resource, relationship);

        Assert.True(subjectSet1 == subjectSet2);
        Assert.False(subjectSet1 != subjectSet2);
        Assert.True(subjectSet1.Equals(subjectSet2));
    }

    [Fact]
    public void Equality_ReturnsFalse_ForDifferentInstances()
    {
        var resource1 = new Resource(PolicyName.From("ns"), Identifier.From("id"));
        var relationship1 = RelationName.From("rel");
        var subjectSet1 = new SubjectSet(resource1, relationship1);

        var resource2 = new Resource(PolicyName.From("ns2"), Identifier.From("id2"));
        var relationship2 = RelationName.From("rel2");
        var subjectSet2 = new SubjectSet(resource2, relationship2);

        Assert.False(subjectSet1 == subjectSet2);
        Assert.True(subjectSet1 != subjectSet2);
        Assert.False(subjectSet1.Equals(subjectSet2));
    }
}
