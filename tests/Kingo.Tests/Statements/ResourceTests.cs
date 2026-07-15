using Kingo.Statements;
using Results;

namespace Kingo.Tests.Statements;

public sealed class ResourceTests
{
    [Fact]
    public void Parse_NamespacedResource_SucceedsAndRoundTrips()
    {
        var result = Resource.Parse("doc:readme");

        var success = Assert.IsType<Result<Resource>.Success>(result);
        Assert.Equal("doc", success.Value.Namespace.Value);
        Assert.Equal("readme", success.Value.Id.Value);
        Assert.Equal("doc:readme", success.Value.ToString());
    }

    [Fact]
    public void Parse_MixedCase_LowercasesNamespaceAndPreservesId()
    {
        var result = Resource.Parse("DOC:ReadMe");

        var success = Assert.IsType<Result<Resource>.Success>(result);
        Assert.Equal("doc", success.Value.Namespace.Value);
        Assert.Equal("ReadMe", success.Value.Id.Value);
        Assert.Equal("doc:ReadMe", success.Value.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ReturnsSingleEmptyError(string input)
    {
        var result = Resource.Parse(input);

        var failure = Assert.IsType<Result<Resource>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal("resource.empty", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Fact]
    public void Parse_NoSeparator_ReturnsSingleFormatError()
    {
        var result = Resource.Parse("docreadme");

        var failure = Assert.IsType<Result<Resource>.Failure>(result);
        Assert.Equal("resource.format", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_EmptyResourceId_ReturnsSingleResourceIdEmptyError()
    {
        var result = Resource.Parse("doc:");

        var failure = Assert.IsType<Result<Resource>.Failure>(result);
        Assert.Equal("resource_id.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_EmptyNamespace_ReturnsSingleNamespaceEmptyError()
    {
        var result = Resource.Parse(":readme");

        var failure = Assert.IsType<Result<Resource>.Failure>(result);
        Assert.Equal("namespace_id.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_BothSidesEmpty_AccumulatesNamespaceThenResourceIdEmpty()
    {
        var result = Resource.Parse(":");

        var failure = Assert.IsType<Result<Resource>.Failure>(result);
        Assert.Equal(["namespace_id.empty", "resource_id.empty"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_SecondColonInId_ReturnsSingleResourceIdInvalidError()
    {
        // split at the FIRST ':' leaves "a:b" as the resource-id, which rejects ':'
        var result = Resource.Parse("doc:a:b");

        var failure = Assert.IsType<Result<Resource>.Failure>(result);
        Assert.Equal("resource_id.invalid", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_BothSidesInvalid_AccumulatesNamespaceThenResourceIdInvalid()
    {
        var result = Resource.Parse("d-c:a:b");

        var failure = Assert.IsType<Result<Resource>.Failure>(result);
        Assert.Equal(["namespace_id.invalid", "resource_id.invalid"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_EqualInputs_ProduceEqualValues()
    {
        var left = Assert.IsType<Result<Resource>.Success>(Resource.Parse("doc:readme")).Value;
        var right = Assert.IsType<Result<Resource>.Success>(Resource.Parse("doc:readme")).Value;

        Assert.Equal(left, right);
        Assert.Equal(new Resource(NamespaceIdentifier.Create("doc"), ResourceIdentifier.Create("readme")), left);
    }

    [Fact]
    public void Parse_DifferentId_ProducesUnequalValues()
    {
        var left = Assert.IsType<Result<Resource>.Success>(Resource.Parse("doc:readme")).Value;
        var right = Assert.IsType<Result<Resource>.Success>(Resource.Parse("doc:license")).Value;

        Assert.NotEqual(left, right);
    }
}
