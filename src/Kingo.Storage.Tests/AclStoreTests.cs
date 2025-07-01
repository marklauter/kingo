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

        var store = AclStore.Empty.Include(
            docSubjectSet.Resource,
            docSubjectSet.Relationship,
            subject);

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

        var store = AclStore.Empty.Include(
            editorSet.Resource,
            editorSet.Relationship,
            subject);

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

        var store = AclStore.Empty.Include(
            ownerSet.Resource,
            ownerSet.Relationship,
            subject);

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

        var store = AclStore.Empty.Include(
            viewerSet.Resource,
            viewerSet.Relationship,
            subject);

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

        var store = AclStore.Empty.Include(
            editorSet.Resource,
            editorSet.Relationship,
            subject);

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

        var store = AclStore.Empty.Include(
            ownerSet.Resource,
            ownerSet.Relationship,
            subject);

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

        var store = AclStore.Empty;

        Assert.False(store.IsAMemberOf(subject, viewerSet, tree));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_ComputedFromParentFolderViewer()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var docResource = new Resource("doc", "readme");
        var folderResource = new Resource("folder", "documents");

        // doc:readme#parent@folder:documents#... (relationship tuple as subject set)
        var store = AclStore.Empty
            .Include(
                docResource,
                "parent",
                new SubjectSet(folderResource, Relationship.Nothing))
            // folder:documents#viewer@subject (membership tuple)
            .Include(
                folderResource,
                "viewer",
                subject);

        var viewerSet = new SubjectSet(docResource, "viewer");

        Assert.True(store.IsAMemberOf(subject, viewerSet, tree));
    }
}
