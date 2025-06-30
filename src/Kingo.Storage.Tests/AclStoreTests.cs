using Kingo.Configuration.Tree;
using Kingo.Facts;

namespace Kingo.Storage.Tests;

public sealed class AclStoreTests
{
    [Fact]
    public async Task IsAMemberOfThisAsync()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var doclResource = new Resource("doc", "readme");
        var docRelationship = Relationship.From("owner");
        var docSubjectSet = new SubjectSet(doclResource, docRelationship);

        var store = new AclStore()
            .Union(docSubjectSet, subject);

        Assert.True(store.IsAMemberOf(subject, docSubjectSet, tree));
    }
}
