namespace Kingo.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_Factory_ReturnsSuccessVariant()
    {
        Result<int> result = Result.Success(42);
        var s = Assert.IsType<Success<int>>(result);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Failure_Factory_ReturnsFailureVariant()
    {
        var error = Error.NotFound("err.x", "msg");
        Result<int> result = Result.Failure<int>(error);
        var f = Assert.IsType<Failure<int>>(result);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<int> result = 42;
        var s = Assert.IsType<Success<int>>(result);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailure()
    {
        var error = Error.Validation("err.input", "bad");
        Result<int> result = error;
        var f = Assert.IsType<Failure<int>>(result);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Match_Success_InvokesOnSuccess()
    {
        Result<int> result = 42;
        var matched = result.Match(
            onSuccess: v => $"value: {v}",
            onError: e => $"error: {e.Code}");
        Assert.Equal("value: 42", matched);
    }

    [Fact]
    public void Match_Failure_InvokesOnError()
    {
        Result<int> result = Error.NotFound("err.x", "msg");
        var matched = result.Match(
            onSuccess: v => $"value: {v}",
            onError: e => $"error: {e.Code}");
        Assert.Equal("error: err.x", matched);
    }

    [Fact]
    public void Switch_Success_InvokesOnSuccess()
    {
        Result<int> result = 42;
        var captured = 0;
        result.Switch(
            onSuccess: v => captured = v,
            onError: _ => Assert.Fail("should not invoke onError"));
        Assert.Equal(42, captured);
    }

    [Fact]
    public void Switch_Failure_InvokesOnError()
    {
        var error = Error.NotFound("err.x", "msg");
        Result<int> result = error;
        Error? captured = null;
        result.Switch(
            onSuccess: _ => Assert.Fail("should not invoke onSuccess"),
            onError: e => captured = e);
        Assert.Equal(error, captured);
    }

    [Fact]
    public void PatternMatch_DispatchesOnSuccessVariant()
    {
        Result<int> result = 42;
        var matched = result switch
        {
            Success<int> s => s.Value,
            Failure<int> => -1,
            _ => -2,
        };
        Assert.Equal(42, matched);
    }

    [Fact]
    public void PatternMatch_DispatchesOnFailureVariant()
    {
        Result<int> result = Error.NotFound("err.x", "msg");
        var code = result switch
        {
            Success<int> => "ok",
            Failure<int> f => f.Error.Code,
            _ => "?",
        };
        Assert.Equal("err.x", code);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForTwoSuccessesWithSameValue()
    {
        Result<int> a = 42;
        Result<int> b = 42;
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForTwoFailuresWithSameError()
    {
        var error = Error.NotFound("err.x", "msg");
        Result<int> a = error;
        Result<int> b = error;
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_ReturnsFalse_BetweenSuccessAndFailure()
    {
        Result<int> success = 42;
        Result<int> failure = Error.NotFound("err.x", "msg");
        Assert.NotEqual(success, failure);
    }

    [Fact]
    public void Apply_Unary_OverSuccess_AppliesFunction()
    {
        Result<int> result = 21;
        var doubled = Result.Apply((int x) => x * 2, result);
        var s = Assert.IsType<Success<int>>(doubled);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Apply_Unary_OverFailure_PassesFailureThrough()
    {
        var error = Error.NotFound("err.x", "msg");
        Result<int> result = error;
        var applied = Result.Apply((int x) => x * 2, result);
        var f = Assert.IsType<Failure<int>>(applied);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Apply_Binary_OverTwoSuccesses_AppliesFunction()
    {
        Result<int> a = 10;
        Result<int> b = 32;
        var sum = Result.Apply((int x, int y) => x + y, a, b);
        var s = Assert.IsType<Success<int>>(sum);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Apply_Binary_FirstArgFailure_ShortCircuitsToFirstError()
    {
        var error = Error.Validation("err.first", "first arg bad");
        Result<int> a = error;
        Result<int> b = 32;
        var applied = Result.Apply((int x, int y) => x + y, a, b);
        var f = Assert.IsType<Failure<int>>(applied);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Apply_Binary_SecondArgFailure_PassesSecondErrorThrough()
    {
        var error = Error.Validation("err.second", "second arg bad");
        Result<int> a = 10;
        Result<int> b = error;
        var applied = Result.Apply((int x, int y) => x + y, a, b);
        var f = Assert.IsType<Failure<int>>(applied);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Apply_Binary_BothArgsFailure_ReturnsFirstError()
    {
        var firstError = Error.Validation("err.first", "first bad");
        var secondError = Error.Validation("err.second", "second bad");
        Result<int> a = firstError;
        Result<int> b = secondError;
        var applied = Result.Apply((int x, int y) => x + y, a, b);
        var f = Assert.IsType<Failure<int>>(applied);
        Assert.Equal(firstError, f.Error);
    }

    [Fact]
    public void Apply_Ternary_OverThreeSuccesses_AppliesFunction()
    {
        Result<int> a = 10;
        Result<int> b = 20;
        Result<int> c = 12;
        var sum = Result.Apply((int x, int y, int z) => x + y + z, a, b, c);
        var s = Assert.IsType<Success<int>>(sum);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Apply_Ternary_AnyFailure_ShortCircuits()
    {
        var error = Error.NotFound("err.x", "msg");
        Result<int> a = 10;
        Result<int> b = error;
        Result<int> c = 12;
        var applied = Result.Apply((int x, int y, int z) => x + y + z, a, b, c);
        var f = Assert.IsType<Failure<int>>(applied);
        Assert.Equal(error, f.Error);
    }
}
