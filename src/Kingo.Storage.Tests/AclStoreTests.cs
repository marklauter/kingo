using Kingo.Facts;

namespace Kingo.Storage.Tests;

public sealed class AclStoreTests
{
    [Fact]
    public async Task IsAMemberOf_Owner_DirectMembership()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var docSubjectSet = new SubjectSet(
            new Resource("doc", "readme"),
            "owner");

        var store = new AclStore().Include(docSubjectSet, subject);

        Assert.True(store.IsAMemberOf(subject, docSubjectSet, tree));
    }

    [Fact]
    public async Task IsAMemberOf_Editor_DirectMembership()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var editorSet = new SubjectSet(
            new Resource("doc", "readme"),
            "editor");

        var store = new AclStore()
            .Include(editorSet, subject);

        Assert.True(store.IsAMemberOf(subject, editorSet, tree));
    }

    [Fact]
    public async Task IsAMemberOf_Editor_ComputedFromOwner()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var resource = new Resource("doc", "readme");
        var ownerSet = new SubjectSet(
            resource,
            "owner");
        var editorSet = new SubjectSet(
            resource,
            "editor");

        var store = new AclStore()
            .Include(ownerSet, subject);

        Assert.True(store.IsAMemberOf(subject, editorSet, tree));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_DirectMembership()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var viewerSet = new SubjectSet(
            new Resource("doc", "readme"),
            "viewer");

        var store = new AclStore()
            .Include(viewerSet, subject);

        Assert.True(store.IsAMemberOf(subject, viewerSet, tree));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_ComputedFromEditor()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var resource = new Resource("doc", "readme");
        var editorSet = new SubjectSet(
            resource,
            "editor");
        var viewerSet = new SubjectSet(
            resource,
            "viewer");

        var store = new AclStore()
            .Include(editorSet, subject);

        Assert.True(store.IsAMemberOf(subject, viewerSet, tree));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_ComputedFromOwnerViaEditor()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var resource = new Resource("doc", "readme");
        var ownerSet = new SubjectSet(
            resource,
            "owner");
        var viewerSet = new SubjectSet(
            resource,
            "viewer");

        var store = new AclStore()
            .Include(ownerSet, subject);

        Assert.True(store.IsAMemberOf(subject, viewerSet, tree));
    }

    [Fact]
    public async Task IsAMemberOf_False_WhenNotIncluded()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var viewerSet = new SubjectSet(
            new Resource("doc", "readme"),
            "viewer");

        var store = new AclStore();

        Assert.False(store.IsAMemberOf(subject, viewerSet, tree));
    }
}
