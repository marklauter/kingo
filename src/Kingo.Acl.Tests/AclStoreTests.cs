using Kingo.Namespaces;
using Kingo.Namespaces.Serializable;
using Kingo.Storage;

namespace Kingo.Acl.Tests;

public sealed class AclStoreTests
{
    private static async Task<DocumentStore> GetPrimedDocumentStoreAsync()
    {
        var store = DocumentStore.Empty();
        var results = new NamespaceWriter(store)
            .Put(await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json"), CancellationToken.None);
        Assert.Equal(3, results.Length);
        Assert.DoesNotContain(NamespaceWriter.PutStatus.TimeoutError, results.Select(i => i.Status));
        Assert.DoesNotContain(NamespaceWriter.PutStatus.DuplicateKeyError, results.Select(i => i.Status));

        results = new NamespaceWriter(store)
            .Put(await NamespaceSpec.FromFileAsync("Data/Namespace.Folder.json"), CancellationToken.None);
        Assert.Equal(3, results.Length);
        Assert.DoesNotContain(NamespaceWriter.PutStatus.TimeoutError, results.Select(i => i.Status));
        Assert.DoesNotContain(NamespaceWriter.PutStatus.DuplicateKeyError, results.Select(i => i.Status));

        return store;
    }

    [Fact]
    public async Task IsAMemberOf_Owner_DirectMembership()
    {
        var subject = new Subject(Guid.NewGuid());
        var docSubjectSet = new SubjectSet(
            new Resource("doc", "readme"),
            "owner");

        var documentStore = await GetPrimedDocumentStoreAsync();
        var writer = new AclWriter(documentStore);
        Assert.Equal(AclWriter.AssociateResponse.Success,
            writer.Associate(
                docSubjectSet.Resource,
                docSubjectSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(new AclReader(documentStore).IsAMemberOf(subject, docSubjectSet));
    }

    [Fact]
    public async Task IsAMemberOf_Editor_DirectMembership()
    {
        var subject = new Subject(Guid.NewGuid());
        var editorSet = new SubjectSet(
            new Resource("doc", "readme"),
            "editor");

        var documentStore = await GetPrimedDocumentStoreAsync();
        var writer = new AclWriter(documentStore);
        Assert.Equal(AclWriter.AssociateResponse.Success,
            writer.Associate(
                editorSet.Resource,
                editorSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(new AclReader(documentStore).IsAMemberOf(subject, editorSet));
    }

    [Fact]
    public async Task IsAMemberOf_Editor_ComputedFromOwner()
    {
        var subject = new Subject(Guid.NewGuid());
        var resource = new Resource("doc", "readme");
        var ownerSet = new SubjectSet(
            resource,
            "owner");
        var editorSet = new SubjectSet(
            resource,
            "editor");

        var documentStore = await GetPrimedDocumentStoreAsync();
        var writer = new AclWriter(documentStore);
        Assert.Equal(AclWriter.AssociateResponse.Success,
            writer.Associate(
                ownerSet.Resource,
                ownerSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(new AclReader(documentStore).IsAMemberOf(subject, editorSet));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_DirectMembership()
    {
        var subject = new Subject(Guid.NewGuid());
        var viewerSet = new SubjectSet(
            new Resource("doc", "readme"),
            "viewer");

        var documentStore = await GetPrimedDocumentStoreAsync();
        var writer = new AclWriter(documentStore);
        Assert.Equal(AclWriter.AssociateResponse.Success,
            writer.Associate(
                viewerSet.Resource,
                viewerSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(new AclReader(documentStore).IsAMemberOf(subject, viewerSet));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_ComputedFromEditor()
    {
        var subject = new Subject(Guid.NewGuid());
        var resource = new Resource("doc", "readme");
        var editorSet = new SubjectSet(
            resource,
            "editor");
        var viewerSet = new SubjectSet(
            resource,
            "viewer");

        var documentStore = await GetPrimedDocumentStoreAsync();
        var writer = new AclWriter(documentStore);
        Assert.Equal(AclWriter.AssociateResponse.Success,
            writer.Associate(
                editorSet.Resource,
                editorSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(new AclReader(documentStore).IsAMemberOf(subject, viewerSet));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_ComputedFromOwnerViaEditor()
    {
        var subject = new Subject(Guid.NewGuid());
        var resource = new Resource("doc", "readme");
        var ownerSet = new SubjectSet(
            resource,
            "owner");
        var viewerSet = new SubjectSet(
            resource,
            "viewer");

        var documentStore = await GetPrimedDocumentStoreAsync();
        var writer = new AclWriter(documentStore);
        Assert.Equal(AclWriter.AssociateResponse.Success,
            writer.Associate(
                ownerSet.Resource,
                ownerSet.Relationship,
                subject,
                CancellationToken.None));

        Assert.True(new AclReader(documentStore).IsAMemberOf(subject, viewerSet));
    }

    [Fact]
    public async Task IsAMemberOf_False_WhenNotIncluded()
    {
        var subject = new Subject(Guid.NewGuid());
        var viewerSet = new SubjectSet(
            new Resource("doc", "readme"),
            "viewer");

        var documentStore = await GetPrimedDocumentStoreAsync();

        Assert.False(new AclReader(documentStore).IsAMemberOf(subject, viewerSet));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_ComputedFromParentFolderViewer()
    {
        var subject = new Subject(Guid.NewGuid());
        var docResource = new Resource("doc", "readme");
        var folderResource = new Resource("folder", "documents");
        var documentStore = await GetPrimedDocumentStoreAsync();
        var writer = new AclWriter(documentStore);

        // doc:readme#parent@folder:documents#... (relationship tuple as subject set)
        Assert.Equal(AclWriter.AssociateResponse.Success,
            writer.Associate(
                docResource,
                "parent",
                new SubjectSet(folderResource, Relationship.Nothing),
                CancellationToken.None));

        // folder:documents#viewer@subject (membership tuple)
        Assert.Equal(AclWriter.AssociateResponse.Success,
            writer.Associate(
                folderResource,
                "viewer",
                subject,
                CancellationToken.None));

        var viewerSet = new SubjectSet(docResource, "viewer");

        Assert.True(new AclReader(documentStore).IsAMemberOf(subject, viewerSet));
    }
}
