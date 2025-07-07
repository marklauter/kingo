using Kingo.Namespaces;
using Kingo.Namespaces.Serializable;
using Kingo.Storage;
using Kingo.Storage.Indexing;

namespace Kingo.Acl.Tests;

public sealed class AclStoreTests
{
    private readonly DocumentIndex index = DocumentIndex.Empty();

    private (DocumentReader reader, DocumentWriter writer) ReaderWriter() =>
        (new(index), new(index));

    private async Task<(DocumentReader reader, DocumentWriter writer)> GetPrimedReaderWriterAsync()
    {
        var (reader, writer) = ReaderWriter();
        var nsWriter = new NamespaceWriter(writer);

        var docSpec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");
        Assert.All(nsWriter.Insert(docSpec, CancellationToken.None), result => Assert.True(result.IsRight));

        var folderSpec = await NamespaceSpec.FromFileAsync("Data/Namespace.Folder.json");
        Assert.All(nsWriter.Insert(folderSpec, CancellationToken.None), result => Assert.True(result.IsRight));

        return (reader, writer);
    }

    [Fact]
    public async Task IsAMemberOf_Owner_DirectMembership()
    {
        var subject = new Subject(1);
        var docSubjectSet = new SubjectSet(
            new Resource("doc", "readme"),
            "owner");

        var (reader, writer) = await GetPrimedReaderWriterAsync();
        var aclWriter = new AclWriter(writer);
        var aclReader = new AclReader(reader);

        Assert.True(aclWriter.Associate(docSubjectSet, subject, CancellationToken.None).IsRight);
        Assert.True(aclReader.IsAMemberOf(subject, docSubjectSet));
    }

    [Fact]
    public async Task IsAMemberOf_Editor_DirectMembership()
    {
        var subject = new Subject(1);
        var editorSet = new SubjectSet(
            new Resource("doc", "readme"),
            "editor");

        var (reader, writer) = await GetPrimedReaderWriterAsync();
        var aclWriter = new AclWriter(writer);
        var aclReader = new AclReader(reader);

        Assert.True(aclWriter.Associate(editorSet, subject, CancellationToken.None).IsRight);
        Assert.True(aclReader.IsAMemberOf(subject, editorSet));
    }

    [Fact]
    public async Task IsAMemberOf_Editor_ComputedFromOwner()
    {
        var subject = new Subject(1);
        var resource = new Resource("doc", "readme");
        var ownerSet = new SubjectSet(
            resource,
            "owner");
        var editorSet = new SubjectSet(
            resource,
            "editor");

        var (reader, writer) = await GetPrimedReaderWriterAsync();
        var aclWriter = new AclWriter(writer);
        var aclReader = new AclReader(reader);

        Assert.True(aclWriter.Associate(ownerSet, subject, CancellationToken.None).IsRight);
        Assert.True(aclReader.IsAMemberOf(subject, editorSet));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_DirectMembership()
    {
        var subject = new Subject(1);
        var viewerSet = new SubjectSet(
            new Resource("doc", "readme"),
            "viewer");

        var (reader, writer) = await GetPrimedReaderWriterAsync();
        var aclWriter = new AclWriter(writer);
        var aclReader = new AclReader(reader);

        Assert.True(aclWriter.Associate(viewerSet, subject, CancellationToken.None).IsRight);
        Assert.True(aclReader.IsAMemberOf(subject, viewerSet));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_ComputedFromEditor()
    {
        var subject = new Subject(1);
        var resource = new Resource("doc", "readme");
        var editorSet = new SubjectSet(
            resource,
            "editor");
        var viewerSet = new SubjectSet(
            resource,
            "viewer");

        var (reader, writer) = await GetPrimedReaderWriterAsync();
        var aclWriter = new AclWriter(writer);
        var aclReader = new AclReader(reader);

        Assert.True(aclWriter.Associate(editorSet, subject, CancellationToken.None).IsRight);
        Assert.True(aclReader.IsAMemberOf(subject, viewerSet));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_ComputedFromOwnerViaEditor()
    {
        var subject = new Subject(1);
        var resource = new Resource("doc", "readme");
        var ownerSet = new SubjectSet(
            resource,
            "owner");
        var viewerSet = new SubjectSet(
            resource,
            "viewer");

        var (reader, writer) = await GetPrimedReaderWriterAsync();
        var aclWriter = new AclWriter(writer);
        var aclReader = new AclReader(reader);

        Assert.True(aclWriter.Associate(ownerSet, subject, CancellationToken.None).IsRight);
        Assert.True(aclReader.IsAMemberOf(subject, viewerSet));
    }

    [Fact]
    public async Task IsAMemberOf_False_WhenNotIncluded()
    {
        var subject = new Subject(1);
        var viewerSet = new SubjectSet(
            new Resource("doc", "readme"),
            "viewer");

        var (reader, _) = await GetPrimedReaderWriterAsync();
        var aclReader = new AclReader(reader);

        Assert.False(aclReader.IsAMemberOf(subject, viewerSet));
    }

    [Fact]
    public async Task IsAMemberOf_Viewer_ComputedFromParentFolderViewer()
    {
        var subject = new Subject(1);
        var docResource = new Resource("doc", "readme");
        var folderResource = new Resource("folder", "documents");

        var (reader, writer) = await GetPrimedReaderWriterAsync();
        var aclWriter = new AclWriter(writer);
        var aclReader = new AclReader(reader);

        // doc:readme#parent@folder:documents#... (relationship tuple as subject set)
        Assert.True(aclWriter.Associate(
            new SubjectSet(docResource, "parent"),
            new SubjectSet(folderResource, Relationship.Nothing),
            CancellationToken.None).IsRight);

        // folder:documents#viewer@subject (membership tuple)
        Assert.True(aclWriter.Associate(
            new SubjectSet(folderResource, "viewer"),
            subject,
            CancellationToken.None).IsRight);

        var viewerSet = new SubjectSet(docResource, "viewer");

        Assert.True(aclReader.IsAMemberOf(subject, viewerSet));
    }
}
