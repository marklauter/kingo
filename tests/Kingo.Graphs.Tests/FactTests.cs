using static Kingo.Graphs.Fact;

namespace Kingo.Graphs.Tests;

public sealed class FactTests
{
    private static SubjectSet Set(string namespacePath, string resourceId, string relationship) =>
        new(
            new Resource(NamespacePath.Unchecked(namespacePath), ResourceId.Unchecked(resourceId)),
            RelationshipName.Unchecked(relationship));

    [Fact]
    public void SubjectFact_HoldsSubjectSetAndSubjectId()
    {
        var set = Set("io/doc", "readme", "viewer");
        var fact = new SubjectFact(set, SubjectId.Unchecked("anne"));

        Assert.Equal(set, fact.SubjectSet);
        Assert.Equal(SubjectId.Unchecked("anne"), fact.Subject);
    }

    [Fact]
    public void SubjectSetFact_HoldsSubjectSetAndSubjectSet()
    {
        var set = Set("io/doc", "readme", "viewer");
        var member = Set("io/team", "sales", "member");
        var fact = new SubjectSetFact(set, member);

        Assert.Equal(set, fact.SubjectSet);
        Assert.Equal(member, fact.Subject);
    }

    [Fact]
    public void ResourceFact_HoldsSubjectSetAndResource()
    {
        var set = Set("io/folder", "x", "parent");
        var resource = new Resource(NamespacePath.Unchecked("io/folder"), ResourceId.Unchecked("y"));
        var fact = new ResourceFact(set, resource);

        Assert.Equal(set, fact.SubjectSet);
        Assert.Equal(resource, fact.Subject);
    }

    [Fact]
    public void Equality_EqualParts_ProduceEqualValues()
    {
        var left = new SubjectFact(Set("io/doc", "readme", "viewer"), SubjectId.Unchecked("anne"));
        var right = new SubjectFact(Set("io/doc", "readme", "viewer"), SubjectId.Unchecked("anne"));

        Assert.Equal(left, right);
    }

    [Fact]
    public void Equality_AcrossFactKinds_ProducesUnequalValues()
    {
        Fact subject = new SubjectFact(Set("io/doc", "readme", "viewer"), SubjectId.Unchecked("anne"));
        Fact subjectSet = new SubjectSetFact(Set("io/doc", "readme", "viewer"), Set("io/team", "sales", "member"));

        Assert.NotEqual(subject, subjectSet);
    }
}
