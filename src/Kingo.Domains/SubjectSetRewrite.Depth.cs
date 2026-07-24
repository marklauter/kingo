using Results;
using System.Collections.Immutable;

namespace Kingo.Domains;

/// <summary>
/// The depth half of the rewrite algebra: the structural bound every operator factory constructs through. It is kept apart from the case declarations in
/// <c>SubjectSetRewrite.cs</c> so the union reads as its six inhabitants and nothing else. The base constructor lives here because <see cref="Depth"/> is the only
/// state the base carries. The constructor is private, which is what closes the case set: only a type nested in <see cref="SubjectSetRewrite"/> can call it.
/// </summary>
public abstract partial record SubjectSetRewrite
{
    /// <summary>
    /// Upper bound on <see cref="Depth"/>, enforced by every operator factory (<c>rewrite.depth</c>). With the bound, no constructible tree can drive a recursion
    /// over the algebra (structural equality, hashing, printing, interpretation) deep enough to exhaust a stack, even a 1MB service thread's. Generous against real
    /// rewrites of a handful of levels. Distinct from the evaluator's fact-driven [[depth-bound]], which counts stored-fact re-entries at runtime, not tree shape.
    /// </summary>
    public const int MaxDepth = 100;

    /// <summary>The tree's structural height: 1 at a leaf, one more than the deepest operand at an operator node. Never exceeds <see cref="MaxDepth"/>.</summary>
    public int Depth { get; }

    private SubjectSetRewrite(int depth) => Depth = depth;

    /// <summary>Builds the refusal a tree past <see cref="MaxDepth"/> receives. Shared by the operator factories and the domain parse edge, one code for one invariant.</summary>
    /// <returns>An <see cref="Error"/> with code <c>rewrite.depth</c>.</returns>
    public static Error DepthError() =>
        Error.Validation("rewrite.depth", $"a rewrite tree deeper than {MaxDepth} levels is refused");

    /// <summary>Returns one more than the deepest operand, the depth an operator node over <paramref name="children"/> would have.</summary>
    /// <returns>One more than the maximum operand depth.</returns>
    private static int DepthOver(ImmutableArray<SubjectSetRewrite> children) => 1 + children.Max(child => child.Depth);

    /// <summary>
    /// Gates every operator factory's construction on depth. Refuses past <see cref="MaxDepth"/> (<c>rewrite.depth</c>), otherwise hands <paramref name="depth"/>
    /// to <paramref name="create"/>, so the bound cannot drift between factories.
    /// </summary>
    /// <returns>
    /// A successful <see cref="Result{T}"/> from <paramref name="create"/> when <paramref name="depth"/> is within <see cref="MaxDepth"/>, otherwise a failure
    /// carrying <c>rewrite.depth</c>.
    /// </returns>
    private static Result<TRewrite> BoundedAt<TRewrite>(int depth, Func<int, TRewrite> create)
        where TRewrite : SubjectSetRewrite =>
        depth > MaxDepth
            ? Result.Failure<TRewrite>(DepthError())
            : Result.Success(create(depth));
}
