namespace Kingo.Storage.Tests;

public sealed class DocumentTypeCacheTests
{
    private sealed record ValidDocument([property: HashKey] string Value);
    private sealed record InvalidDocument(string Value);

    [Name("named")]
    private sealed record NamedDocument([property: HashKey] string Value);
    private sealed record UnnamedDocument([property: HashKey] string Value);

    [Fact]
    public void ValidWhenHasHashKey() =>
        Assert.Equal(nameof(ValidDocument), DocumentTypeCache<ValidDocument>.Name);

    [Fact]
    public void InvalidWhenMissingHashKey() =>
        _ = Assert.Throws<TypeInitializationException>(() => { var x = DocumentTypeCache<InvalidDocument>.Name; });

    [Fact]
    public void NamedDocumentName() =>
        Assert.Equal("named", DocumentTypeCache<NamedDocument>.Name);

    [Fact]
    public void UnnamedDocumentName() =>
        Assert.Equal(nameof(UnnamedDocument), DocumentTypeCache<UnnamedDocument>.Name);
}

