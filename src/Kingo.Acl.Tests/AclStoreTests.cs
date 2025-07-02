using Kingo.Facts;
using Kingo.Storage;

namespace Kingo.Acl.Tests;

public sealed class AclStoreTests
{
    [Fact]
    public async Task IsAMemberOfOwnerDirectMembership()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var docSubjectSet = new SubjectSet(
            new Resource("doc", "readme"),
            "owner");

        var store = new AclStore(DocumentStore.Empty());
        Assert.Equal(AclStore.AssociateResponse.Success,
            store.Associate(
                docSubjectSet.Resource,
                docSubjectSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(store.IsAMemberOf(subject, docSubjectSet, tree));
    }

    [Fact]
    public async Task IsAMemberOfEditorDirectMembership()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var editorSet = new SubjectSet(
            new Resource("doc", "readme"),
            "editor");

        var store = new AclStore(DocumentStore.Empty());
        Assert.Equal(AclStore.AssociateResponse.Success,
            store.Associate(
                editorSet.Resource,
                editorSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(store.IsAMemberOf(subject, editorSet, tree));
    }

    [Fact]
    public async Task IsAMemberOfEditorComputedFromOwner()
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

        var store = new AclStore(DocumentStore.Empty());
        Assert.Equal(AclStore.AssociateResponse.Success,
            store.Associate(
                ownerSet.Resource,
                ownerSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(store.IsAMemberOf(subject, editorSet, tree));
    }

    [Fact]
    public async Task IsAMemberOfViewerDirectMembership()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var viewerSet = new SubjectSet(
            new Resource("doc", "readme"),
            "viewer");

        var store = new AclStore(DocumentStore.Empty());
        Assert.Equal(AclStore.AssociateResponse.Success,
            store.Associate(
                viewerSet.Resource,
                viewerSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(store.IsAMemberOf(subject, viewerSet, tree));
    }

    [Fact]
    public async Task IsAMemberOfViewerComputedFromEditor()
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

        var store = new AclStore(DocumentStore.Empty());
        Assert.Equal(AclStore.AssociateResponse.Success,
            store.Associate(
                editorSet.Resource,
                editorSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(store.IsAMemberOf(subject, viewerSet, tree));
    }

    [Fact]
    public async Task IsAMemberOfViewerComputedFromOwnerViaEditor()
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

        var store = new AclStore(DocumentStore.Empty());
        Assert.Equal(AclStore.AssociateResponse.Success,
            store.Associate(
                ownerSet.Resource,
                ownerSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(store.IsAMemberOf(subject, viewerSet, tree));
    }

    [Fact]
    public async Task IsAMemberOfFalseWhenNotIncluded()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var viewerSet = new SubjectSet(
            new Resource("doc", "readme"),
            "viewer");

        var store = new AclStore(DocumentStore.Empty());

        Assert.False(store.IsAMemberOf(subject, viewerSet, tree));
    }

    [Fact]
    public async Task IsAMemberOfViewerComputedFromParentFolderViewer()
    {
        var subject = new Subject(Guid.NewGuid());
        var tree = await NamespaceTree.FromFileAsync("NamespaceConfiguration.json");

        var docResource = new Resource("doc", "readme");
        var folderResource = new Resource("folder", "documents");

        // doc:readme#parent@folder:documents#... (relationship tuple as subject set)
        var store = new AclStore(DocumentStore.Empty());

        Assert.Equal(AclStore.AssociateResponse.Success,
            store.Associate(
                docResource,
                "parent",
                new SubjectSet(folderResource, Relationship.Nothing),
                CancellationToken.None));

        // folder:documents#viewer@subject (membership tuple)
        Assert.Equal(AclStore.AssociateResponse.Success,
            store.Associate(
                folderResource,
                "viewer",
                subject,
                CancellationToken.None));

        var viewerSet = new SubjectSet(docResource, "viewer");

        var isMember = store.IsAMemberOf(subject, viewerSet, tree);
        Assert.True(isMember);
    }
}
