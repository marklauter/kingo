namespace Kingo.Facts.Tests;

public sealed class ResourceTests
{
    [Fact]
    public void Construction_HoldsNamespaceAndId()
    {
        var resource = new Resource(NamespacePath.Unchecked("io/doc"), ResourceId.Unchecked("readme"));

        Assert.Equal(NamespacePath.Unchecked("io/doc"), resource.Namespace);
        Assert.Equal(ResourceId.Unchecked("readme"), resource.Id);
    }

    [Fact]
    public void Equality_EqualParts_ProduceEqualValues()
    {
        var left = new Resource(NamespacePath.Unchecked("io/doc"), ResourceId.Unchecked("readme"));
        var right = new Resource(NamespacePath.Unchecked("io/doc"), ResourceId.Unchecked("readme"));

        Assert.Equal(left, right);
    }

    [Fact]
    public void Equality_DifferentId_ProducesUnequalValues()
    {
        var left = new Resource(NamespacePath.Unchecked("io/doc"), ResourceId.Unchecked("readme"));
        var right = new Resource(NamespacePath.Unchecked("io/doc"), ResourceId.Unchecked("license"));

        Assert.NotEqual(left, right);
    }
}
