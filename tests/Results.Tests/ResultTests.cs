namespace Results.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_Factory_ReturnsSuccessVariant()
    {
        var result = Result.Success(42);
        var s = Assert.IsType<Result<int>.Success>(result);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Failure_Factory_ReturnsFailureVariant()
    {
        var error = Error.NotFound("err.x", "msg");
        var result = Result.Failure<int>(error);
        var f = Assert.IsType<Result<int>.Failure>(result);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Match_Success_InvokesOnSuccess()
    {
        var result = Result.Success(42);
        var matched = result.Match(
            onSuccess: v => $"value: {v}",
            onError: e => $"error: {e.Code}");
        Assert.Equal("value: 42", matched);
    }

    [Fact]
    public void Match_Failure_InvokesOnError()
    {
        var result = Result.Failure<int>(Error.NotFound("err.x", "msg"));
        var matched = result.Match(
            onSuccess: v => $"value: {v}",
            onError: e => $"error: {e.Code}");
        Assert.Equal("error: err.x", matched);
    }

    [Fact]
    public void Map_OverSuccess_TransformsValue()
    {
        var result = Result.Success(21);
        var mapped = result.Map(x => x * 2);
        var s = Assert.IsType<Result<int>.Success>(mapped);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Map_OverFailure_PassesFailureThrough()
    {
        var error = Error.NotFound("err.x", "msg");
        var result = Result.Failure<int>(error);
        var mapped = result.Map(x => x * 2);
        var f = Assert.IsType<Result<int>.Failure>(mapped);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Map_TypeChange_TransformsToNewType()
    {
        var result = Result.Success(42);
        var mapped = result.Map(x => $"value: {x}");
        var s = Assert.IsType<Result<string>.Success>(mapped);
        Assert.Equal("value: 42", s.Value);
    }

    [Fact]
    public void Bind_OverSuccess_RunsContinuation()
    {
        var result = Result.Success(21);
        var bound = result.Bind(x => Result.Success(x * 2));
        var s = Assert.IsType<Result<int>.Success>(bound);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Bind_OverSuccessReturningFailure_PropagatesInnerFailure()
    {
        var inner = Error.Validation("err.range", "out of range");
        var result = Result.Success(21);
        var bound = result.Bind(_ => Result.Failure<int>(inner));
        var f = Assert.IsType<Result<int>.Failure>(bound);
        Assert.Equal(inner, f.Error);
    }

    [Fact]
    public void Bind_OverFailure_SkipsContinuation()
    {
        var error = Error.NotFound("err.outer", "outer failure");
        var result = Result.Failure<int>(error);
        var invoked = false;
        var bound = result.Bind(x =>
        {
            invoked = true;
            return Result.Success(x * 2);
        });
        Assert.False(invoked);
        var f = Assert.IsType<Result<int>.Failure>(bound);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public async Task BindAsync_OverSuccess_AwaitsAndReturnsContinuation()
    {
        var result = Result.Success(21);
        var bound = await result.BindAsync(async x =>
        {
            await Task.Yield();
            return Result.Success(x * 2);
        });
        var s = Assert.IsType<Result<int>.Success>(bound);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public async Task BindAsync_OverFailure_SkipsContinuation()
    {
        var error = Error.NotFound("err.outer", "outer failure");
        var result = Result.Failure<int>(error);
        var invoked = false;
        var bound = await result.BindAsync(async _ =>
        {
            invoked = true;
            await Task.Yield();
            return Result.Success(0);
        });
        Assert.False(invoked);
        var f = Assert.IsType<Result<int>.Failure>(bound);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public async Task BindAsync_OverSuccessReturningFailure_PropagatesInnerFailure()
    {
        var inner = Error.Validation("err.range", "out of range");
        var result = Result.Success(21);
        var bound = await result.BindAsync(async _ =>
        {
            await Task.Yield();
            return Result.Failure<int>(inner);
        });
        var f = Assert.IsType<Result<int>.Failure>(bound);
        Assert.Equal(inner, f.Error);
    }

    [Fact]
    public void PatternMatch_DispatchesOnSuccessVariant()
    {
        var result = Result.Success(42);
        var matched = result switch
        {
            Result<int>.Success s => s.Value,
            Result<int>.Failure => -1,
            _ => -2,
        };
        Assert.Equal(42, matched);
    }

    [Fact]
    public void PatternMatch_DispatchesOnFailureVariant()
    {
        var result = Result.Failure<int>(Error.NotFound("err.x", "msg"));
        var code = result switch
        {
            Result<int>.Success => "ok",
            Result<int>.Failure f => f.Error.Code,
            _ => "?",
        };
        Assert.Equal("err.x", code);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForTwoSuccessesWithSameValue()
    {
        var a = Result.Success(42);
        var b = Result.Success(42);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForTwoFailuresWithSameError()
    {
        var error = Error.NotFound("err.x", "msg");
        var a = Result.Failure<int>(error);
        var b = Result.Failure<int>(error);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_ReturnsFalse_BetweenSuccessAndFailure()
    {
        var success = Result.Success(42);
        var failure = Result.Failure<int>(Error.NotFound("err.x", "msg"));
        Assert.NotEqual(success, failure);
    }

    [Fact]
    public void Apply_OverWrappedFunctionAndWrappedSuccess_AppliesFunction()
    {
        var doubler = Result.Success<Func<int, int>>(x => x * 2);
        var arg = Result.Success(21);

        var applied = Result.Apply(doubler, arg);

        var s = Assert.IsType<Result<int>.Success>(applied);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Apply_WrappedFunctionFailure_PassesFunctionErrorThrough()
    {
        var error = Error.Unexpected("err.fn", "function unavailable");
        var doubler = Result.Failure<Func<int, int>>(error);
        var arg = Result.Success(21);

        var applied = Result.Apply(doubler, arg);

        var f = Assert.IsType<Result<int>.Failure>(applied);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Apply_WrappedArgFailure_PassesArgErrorThrough()
    {
        var doubler = Result.Success<Func<int, int>>(x => x * 2);
        var error = Error.NotFound("err.arg", "arg missing");
        var arg = Result.Failure<int>(error);

        var applied = Result.Apply(doubler, arg);

        var f = Assert.IsType<Result<int>.Failure>(applied);
        Assert.Equal(error, f.Error);
    }

    [Fact]
    public void Apply_BothFailures_ShortCircuitsToFunctionError()
    {
        var fnError = Error.Unexpected("err.fn", "function unavailable");
        var argError = Error.NotFound("err.arg", "arg missing");
        var doubler = Result.Failure<Func<int, int>>(fnError);
        var arg = Result.Failure<int>(argError);

        var applied = Result.Apply(doubler, arg);

        var f = Assert.IsType<Result<int>.Failure>(applied);
        Assert.Equal(fnError, f.Error);
    }

    [Fact]
    public void Apply_BinaryViaCurry_OverTwoSuccesses_AppliesFunction()
    {
        // Curry a binary function and chain Apply twice — the canonical applicative pattern.
        var curriedAdd = Result.Success<Func<int, Func<int, int>>>(x => y => x + y);
        var a = Result.Success(10);
        var b = Result.Success(32);

        var sum = Result.Apply(Result.Apply(curriedAdd, a), b);

        var s = Assert.IsType<Result<int>.Success>(sum);
        Assert.Equal(42, s.Value);
    }

    [Fact]
    public void Apply_BinaryViaCurry_FirstArgFailure_ShortCircuits()
    {
        var curriedAdd = Result.Success<Func<int, Func<int, int>>>(x => y => x + y);
        var error = Error.Validation("err.first", "first arg bad");
        var a = Result.Failure<int>(error);
        var b = Result.Success(32);

        var sum = Result.Apply(Result.Apply(curriedAdd, a), b);

        var f = Assert.IsType<Result<int>.Failure>(sum);
        Assert.Equal(error, f.Error);
    }
}
