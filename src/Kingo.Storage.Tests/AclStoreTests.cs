using Kingo.Facts;

namespace Kingo.Storage.Tests;

public class AclStoreTests
{
    [Fact]
    public void IsAMemberOf()
    {
        var ns = new Namespace("file");
        var resource = new Resource(ns, "readme");
        var relationship = new Relationship("owner");
        var subject = new Subject(Guid.NewGuid());
        var store = new AclStore()
            .Union(resource, relationship, subject);

        var subjectSet = new SubjectSet(resource, relationship);
        Assert.True(store.IsAMemberOf(subject, subjectSet));
    }
}
