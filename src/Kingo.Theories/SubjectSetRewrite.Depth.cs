using Results;
using System.Collections.Immutable;

namespace Kingo.Theories;

public abstract partial record SubjectSetRewrite
{
    public const int MaxDepth = 100;

    public int Depth { get; }

    private SubjectSetRewrite(int depth) => Depth = depth;

    public static Error DepthError() =>
        Error.Validation(Diagnostics.ErrorCodes.Rewrite.Depth, $"a rewrite tree deeper than {MaxDepth} levels is refused");

    private static int DepthOver(ImmutableArray<SubjectSetRewrite> children) => 1 + children.Max(child => child.Depth);

    private static Result<TRewrite> BoundedAt<TRewrite>(int depth, Func<int, TRewrite> create)
        where TRewrite : SubjectSetRewrite =>
        depth > MaxDepth
            ? Result.Failure<TRewrite>(DepthError())
            : Result.Success(create(depth));
}
