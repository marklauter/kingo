namespace Kingo.Tests;

public sealed class ResourceTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        var @namespace = Namespace.From("ns");
        var identifier = Identifier.From("id");

        var resource = new Resource(@namespace, identifier);

        Assert.Equal(@namespace, resource.Namespace);
        Assert.Equal(identifier, resource.Name);
    }

    [Fact]
    public void Equality_ReturnsTrue_ForEqualInstances()
    {
        var namespace1 = Namespace.From("ns");
        var identifier1 = Identifier.From("id");
        var resource1 = new Resource(namespace1, identifier1);

        var namespace2 = Namespace.From("ns");
        var identifier2 = Identifier.From("id");
        var resource2 = new Resource(namespace2, identifier2);

        Assert.True(resource1 == resource2);
        Assert.False(resource1 != resource2);
        Assert.True(resource1.Equals(resource2));
    }

    [Fact]
    public void Equality_ReturnsFalse_ForDifferentInstances()
    {
        var namespace1 = Namespace.From("ns1");
        var identifier1 = Identifier.From("id1");
        var resource1 = new Resource(namespace1, identifier1);

        var namespace2 = Namespace.From("ns2");
        var identifier2 = Identifier.From("id2");
        var resource2 = new Resource(namespace2, identifier2);

        Assert.False(resource1 == resource2);
        Assert.True(resource1 != resource2);
        Assert.False(resource1.Equals(resource2));
    }
}
