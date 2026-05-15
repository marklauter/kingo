namespace Results.Tests;

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
    public void Map_OverSuccess_TransformsValue()
    {
        Result<int> result = 21;
        var mapped = result.Map(x => x * 2);
        var s = Assert.IsType<Success<int>>(mapped);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Map_OverFailure_PassesFailureThrough()
    {
        var error = Error.NotFound("err.x", "msg");
        Result<int> result = error;
        var mapped = result.Map(x => x * 2);
        var f = Assert.IsType<Failure<int>>(mapped);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Map_TypeChange_TransformsToNewType()
    {
        Result<int> result = 42;
        var mapped = result.Map(x => $"value: {x}");
        var s = Assert.IsType<Success<string>>(mapped);
        Assert.Equal("value: 42", s.Value);
    }

    [Fact]
    public void Bind_OverSuccess_RunsContinuation()
    {
        Result<int> result = 21;
        var bound = result.Bind(x => Result.Success(x * 2));
        var s = Assert.IsType<Success<int>>(bound);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Bind_OverSuccessReturningFailure_PropagatesInnerFailure()
    {
        var inner = Error.Validation("err.range", "out of range");
        Result<int> result = 21;
        var bound = result.Bind<int>(_ => inner);
        var f = Assert.IsType<Failure<int>>(bound);
        Assert.Equal(inner, f.Error);
    }

    [Fact]
    public void Bind_OverFailure_SkipsContinuation()
    {
        var error = Error.NotFound("err.outer", "outer failure");
        Result<int> result = error;
        var invoked = false;
        var bound = result.Bind(x =>
        {
            invoked = true;
            return Result.Success(x * 2);
        });
        Assert.False(invoked);
        var f = Assert.IsType<Failure<int>>(bound);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public async Task BindAsync_OverSuccess_AwaitsAndReturnsContinuation()
    {
        Result<int> result = 21;
        var bound = await result.BindAsync(async x =>
        {
            await Task.Yield();
            return Result.Success(x * 2);
        });
        var s = Assert.IsType<Success<int>>(bound);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public async Task BindAsync_OverFailure_SkipsContinuation()
    {
        var error = Error.NotFound("err.outer", "outer failure");
        Result<int> result = error;
        var invoked = false;
        var bound = await result.BindAsync<int>(async _ =>
        {
            invoked = true;
            await Task.Yield();
            return Result.Success(0);
        });
        Assert.False(invoked);
        var f = Assert.IsType<Failure<int>>(bound);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public async Task BindAsync_OverSuccessReturningFailure_PropagatesInnerFailure()
    {
        var inner = Error.Validation("err.range", "out of range");
        Result<int> result = 21;
        var bound = await result.BindAsync<int>(async _ =>
        {
            await Task.Yield();
            return inner;
        });
        var f = Assert.IsType<Failure<int>>(bound);
        Assert.Equal(inner, f.Error);
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
    public void Apply_OverWrappedFunctionAndWrappedSuccess_AppliesFunction()
    {
        Result<Func<int, int>> doubler = Result.Success<Func<int, int>>(x => x * 2);
        Result<int> arg = 21;

        var applied = Result.Apply(doubler, arg);

        var s = Assert.IsType<Success<int>>(applied);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Apply_WrappedFunctionFailure_PassesFunctionErrorThrough()
    {
        var error = Error.Unexpected("err.fn", "function unavailable");
        Result<Func<int, int>> doubler = error;
        Result<int> arg = 21;

        var applied = Result.Apply(doubler, arg);

        var f = Assert.IsType<Failure<int>>(applied);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Apply_WrappedArgFailure_PassesArgErrorThrough()
    {
        Result<Func<int, int>> doubler = Result.Success<Func<int, int>>(x => x * 2);
        var error = Error.NotFound("err.arg", "arg missing");
        Result<int> arg = error;

        var applied = Result.Apply(doubler, arg);

        var f = Assert.IsType<Failure<int>>(applied);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Apply_BothFailures_ShortCircuitsToFunctionError()
    {
        var fnError = Error.Unexpected("err.fn", "function unavailable");
        var argError = Error.NotFound("err.arg", "arg missing");
        Result<Func<int, int>> doubler = fnError;
        Result<int> arg = argError;

        var applied = Result.Apply(doubler, arg);

        var f = Assert.IsType<Failure<int>>(applied);
        Assert.Equal(fnError, f.Error);
    }

    [Fact]
    public void Apply_BinaryViaCurry_OverTwoSuccesses_AppliesFunction()
    {
        // Curry a binary function and chain Apply twice — the canonical applicative pattern.
        Result<Func<int, Func<int, int>>> curriedAdd = Result.Success<Func<int, Func<int, int>>>(x => y => x + y);
        Result<int> a = 10;
        Result<int> b = 32;

        var sum = Result.Apply(Result.Apply(curriedAdd, a), b);

        var s = Assert.IsType<Success<int>>(sum);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Apply_BinaryViaCurry_FirstArgFailure_ShortCircuits()
    {
        Result<Func<int, Func<int, int>>> curriedAdd = Result.Success<Func<int, Func<int, int>>>(x => y => x + y);
        var error = Error.Validation("err.first", "first arg bad");
        Result<int> a = error;
        Result<int> b = 32;

        var sum = Result.Apply(Result.Apply(curriedAdd, a), b);

        var f = Assert.IsType<Failure<int>>(sum);
        Assert.Equal(error, f.Error);
    }
}
