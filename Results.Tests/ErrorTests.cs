namespace Results.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void Validation_CreatesErrorWithValidationType()
    {
        var error = Error.Validation("err.input", "input is invalid");
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("err.input", error.Code);
        Assert.Equal("input is invalid", error.Message);
    }

    [Fact]
    public void NotFound_CreatesErrorWithNotFoundType()
    {
        var error = Error.NotFound("err.tuple.not_found", "tuple not found");
        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("err.tuple.not_found", error.Code);
        Assert.Equal("tuple not found", error.Message);
    }

    [Fact]
    public void Gone_CreatesErrorWithGoneType()
    {
        var error = Error.Gone("err.policy.gone", "policy was deleted");
        Assert.Equal(ErrorType.Gone, error.Type);
        Assert.Equal("err.policy.gone", error.Code);
        Assert.Equal("policy was deleted", error.Message);
    }

    [Fact]
    public void Conflict_CreatesErrorWithConflictType()
    {
        var error = Error.Conflict("err.version.conflict", "version mismatch");
        Assert.Equal(ErrorType.Conflict, error.Type);
    }

    [Fact]
    public void Unexpected_CreatesErrorWithUnexpectedType()
    {
        var error = Error.Unexpected("err.boom", "something went wrong");
        Assert.Equal(ErrorType.Unexpected, error.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Factory_Throws_IfCodeIsNullOrWhitespace(string? code) =>
        Assert.ThrowsAny<ArgumentException>(() => Error.NotFound(code!, "message"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Factory_Throws_IfMessageIsNullOrWhitespace(string? message) =>
        Assert.ThrowsAny<ArgumentException>(() => Error.NotFound("err.x", message!));

    [Fact]
    public void Equals_ReturnsTrue_ForSameTypeAndCodeAndMessage()
    {
        var a = Error.NotFound("err.x", "msg");
        var b = Error.NotFound("err.x", "msg");
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenTypeDiffers()
    {
        var a = Error.NotFound("err.x", "msg");
        var b = Error.Gone("err.x", "msg");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenCodeDiffers()
    {
        var a = Error.NotFound("err.x", "msg");
        var b = Error.NotFound("err.y", "msg");
        Assert.NotEqual(a, b);
    }
}
