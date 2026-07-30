using Results;
using System.Collections.Immutable;

namespace Kingo.Theories;

public sealed record Namespace
{
    public NamespaceName Name { get; }

    public ImmutableArray<Relation> Relations { get; }

    private Namespace(NamespaceName name, ImmutableArray<Relation> relations)
    {
        Name = name;
        Relations = relations;
    }

    public static Result<Namespace> Create(NamespaceName name, ImmutableArray<Relation> relations)
    {
        if (relations.IsDefault)
            relations = [];

        var duplicates = relations
            .GroupBy(relation => relation.Name)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                Diagnostics.ErrorCodes.Namespace.DuplicateRelation,
                ErrorMessage.Unchecked($"relation '{group.Key}' is defined more than once in namespace '{name}'")))
            .ToImmutableArray();
        if (!duplicates.IsEmpty)
            return Result.Failure<Namespace>(duplicates);

        var references = relations.ToImmutableDictionary(
            relation => relation.Name,
            relation => IntraNamespaceReferences(relation.Rewrite).Distinct().ToImmutableArray());

        var defined = relations.Select(relation => relation.Name).ToImmutableHashSet();
        var dangling = relations
            .SelectMany(relation => references[relation.Name]
                .Select(reference => reference.Target)
                .Distinct()
                .Where(target => !defined.Contains(target))
                .Select(target => Error.Validation(
                    Diagnostics.ErrorCodes.Namespace.DanglingReference,
                    ErrorMessage.Unchecked($"relation '{relation.Name}' references '{target}', which is not defined in namespace '{name}'"))))
            .ToImmutableArray();
        if (!dangling.IsEmpty)
            return Result.Failure<Namespace>(dangling);

        var cycles = DetectCycles(name, relations, references);
        return cycles.IsEmpty
            ? Result.Success(new Namespace(name, relations))
            : Result.Failure<Namespace>(cycles);
    }

    private static IEnumerable<SubjectSetRewrite> Flatten(SubjectSetRewrite rewrite)
    {
        var pending = new Stack<SubjectSetRewrite>();
        pending.Push(rewrite);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            yield return node;
            var children = Children(node);
            for (var i = children.Length - 1; i >= 0; i--)
                pending.Push(children[i]);
        }
    }

    private static ImmutableArray<SubjectSetRewrite> Children(SubjectSetRewrite rewrite) =>
        rewrite switch
        {
            SubjectSetRewrite.Union union => union.Children,
            SubjectSetRewrite.Intersection intersection => intersection.Children,
            SubjectSetRewrite.Exclusion exclusion => [exclusion.Include, exclusion.Exclude],
            SubjectSetRewrite.This or SubjectSetRewrite.ComputedSubjectSet => [],
            _ => LeafChildren((SubjectSetRewrite.FactToSubjectSet)rewrite),
        };

    private static ImmutableArray<SubjectSetRewrite> LeafChildren(SubjectSetRewrite.FactToSubjectSet _) => [];

    private static IEnumerable<(RelationName Target, bool IsZeroFactEdge)> IntraNamespaceReferences(SubjectSetRewrite rewrite) =>
        Flatten(rewrite).SelectMany(IEnumerable<(RelationName, bool)> (node) => node switch
        {
            SubjectSetRewrite.ComputedSubjectSet computed => [(computed.Relation, true)],
            SubjectSetRewrite.FactToSubjectSet factTo => [(factTo.FactsetRelation, false)],
            _ => [],
        });

    private static ImmutableArray<Error> DetectCycles(
        NamespaceName name,
        ImmutableArray<Relation> relations,
        ImmutableDictionary<RelationName, ImmutableArray<(RelationName Target, bool IsZeroFactEdge)>> references)
    {
        var edges = references.ToImmutableDictionary(
            entry => entry.Key,
            entry => entry.Value
                .Where(reference => reference.IsZeroFactEdge)
                .Select(reference => reference.Target)
                .ToImmutableArray());

        var errors = ImmutableArray.CreateBuilder<Error>();
        var finished = new HashSet<RelationName>();
        var path = new List<RelationName>();
        var onPath = new HashSet<RelationName>();
        var frames = new Stack<(RelationName Node, int NextEdge)>();

        foreach (var relation in relations)
        {
            if (finished.Contains(relation.Name))
                continue;

            frames.Push((relation.Name, 0));
            _ = onPath.Add(relation.Name);
            path.Add(relation.Name);

            while (frames.Count > 0)
            {
                var (node, nextEdge) = frames.Pop();
                var targets = edges[node];
                if (nextEdge == targets.Length)
                {
                    path.RemoveAt(path.Count - 1);
                    _ = onPath.Remove(node);
                    _ = finished.Add(node);
                    continue;
                }

                frames.Push((node, nextEdge + 1));
                var target = targets[nextEdge];
                if (finished.Contains(target))
                    continue;

                if (onPath.Contains(target))
                {
                    var cycle = path.Skip(path.IndexOf(target)).Append(target);
                    errors.Add(Error.Validation(
                        Diagnostics.ErrorCodes.Namespace.RewriteCycle,
                        ErrorMessage.Unchecked($"rewrite cycle in namespace '{name}': {string.Join(" -> ", cycle.Select(step => $"'{step}'"))}")));
                    continue;
                }

                frames.Push((target, 0));
                _ = onPath.Add(target);
                path.Add(target);
            }
        }

        return errors.ToImmutable();
    }

    public bool Equals(Namespace? other) =>
        other is not null
        && Name.Equals(other.Name)
        && Relations.AsSpan().SequenceEqual(other.Relations.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, SequenceHash.Of(Relations));
}
