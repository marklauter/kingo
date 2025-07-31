//namespace Kingo.Policies.Tests;

//public sealed class ResourceTests
//{
//    [Fact]
//    public void Constructor_InitializesProperties()
//    {
//        var policy = NamespaceIdentifier.From("ns");
//        var identifier = Identifier.From("id");

//        var resource = new Resource(policy, identifier);

//        Assert.Equal(policy, resource.Policy);
//        Assert.Equal(identifier, resource.Name);
//    }

//    [Fact]
//    public void Equality_ReturnsTrue_ForEqualInstances()
//    {
//        var policy1 = NamespaceIdentifier.From("ns");
//        var identifier1 = Identifier.From("id");
//        var resource1 = new Resource(policy1, identifier1);

//        var policy2 = NamespaceIdentifier.From("ns");
//        var identifier2 = Identifier.From("id");
//        var resource2 = new Resource(policy2, identifier2);

//        Assert.True(resource1 == resource2);
//        Assert.False(resource1 != resource2);
//        Assert.True(resource1.Equals(resource2));
//    }

//    [Fact]
//    public void Equality_ReturnsFalse_ForDifferentInstances()
//    {
//        var policy1 = NamespaceIdentifier.From("ns1");
//        var identifier1 = Identifier.From("id1");
//        var resource1 = new Resource(policy1, identifier1);

//        var policy2 = NamespaceIdentifier.From("ns2");
//        var identifier2 = Identifier.From("id2");
//        var resource2 = new Resource(policy2, identifier2);

//        Assert.False(resource1 == resource2);
//        Assert.True(resource1 != resource2);
//        Assert.False(resource1.Equals(resource2));
//    }
//}
