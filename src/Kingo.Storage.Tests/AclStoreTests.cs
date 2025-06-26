using Kingo.Facts;

namespace Kingo.Storage.Tests;

public sealed class AclStoreTests
{
    [Fact]
    public void SimpleIsAMemberOf()
    {
        var fileNamespace = new Namespace("file");
        var fileResource = new Resource(fileNamespace, "readme");
        var fileRelationship = new Relationship("owner");
        var fileSubjectSet = new SubjectSet(fileResource, fileRelationship);
        var subject = new Subject(Guid.NewGuid());

        var store = new AclStore()
            .Union(fileResource, fileRelationship, subject);

        Assert.True(store.IsAMemberOf(subject, fileSubjectSet));
    }

    [Fact]
    public void RecursiveIsAMemberOf()
    {
        // link the subject to the team
        var teamNamespace = new Namespace("team");
        var teamResource = new Resource(teamNamespace, "editor");
        var teamRelationship = new Relationship("member");
        var subject = new Subject(Guid.NewGuid());
        var teamSubjectSet = new SubjectSet(teamResource, teamRelationship);
        var store = new AclStore()
            .Union(teamResource, teamRelationship, subject);

        // verify simple subject membership
        Assert.True(store.IsAMemberOf(subject, teamSubjectSet));

        // link team subjectset to readme file
        var fileNamespace = new Namespace("file");
        var fileResource = new Resource(fileNamespace, "readme");
        var fileRelationship = new Relationship("owner");
        var fileSubjectSet = new SubjectSet(fileResource, fileRelationship);
        store = store
            .Union(fileResource, fileRelationship, teamSubjectSet);

        // verify recursive subject membership
        Assert.True(store.IsAMemberOf(subject, fileSubjectSet));
    }
}
