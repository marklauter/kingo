using Kingo.Facts;

namespace Kingo.Storage.Tests;

public sealed class AclStoreTests
{
    [Fact]
    public void SimpleIsAMemberOf()
    {
        var fileResource = new Resource("file", "readme");
        var fileRelationship = Relationship.From("owner");
        var fileSubjectSet = new SubjectSet(fileResource, fileRelationship);
        var subject = new Subject(Guid.NewGuid());

        var store = new AclStore()
            .Union(fileSubjectSet, subject);

        Assert.True(store.IsAMemberOf(subject, fileSubjectSet));
    }

    [Fact]
    public void RecursiveIsAMemberOf()
    {
        // link the subject to the team
        var teamResource = new Resource("team", "editor");
        var teamRelationship = Relationship.From("member");
        var subject = new Subject(Guid.NewGuid());
        var teamSubjectSet = new SubjectSet(teamResource, teamRelationship);
        var store = new AclStore()
            .Union(teamSubjectSet, subject);

        // verify simple subject membership
        Assert.True(store.IsAMemberOf(subject, teamSubjectSet));

        // link team subjectset to readme file
        var fileResource = new Resource("file", "readme");
        var fileRelationship = Relationship.From("owner");
        var fileSubjectSet = new SubjectSet(fileResource, fileRelationship);
        store = store
            .Union(fileSubjectSet, teamSubjectSet);

        // verify recursive subject membership
        Assert.True(store.IsAMemberOf(subject, fileSubjectSet));
    }
}
