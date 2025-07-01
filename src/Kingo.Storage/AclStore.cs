using Kingo.Facts;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Storage;

// todo: convert AclStore to use the document store
// to prove it can be performed with dynamodb or cassandra

// <summary>
/// This is a demo store. A production store would use DynamoDB, Casandra, or other versioned key-value store.
/// </summary>
public sealed class AclStore
{
    private sealed class SubjectMap
    {
        private readonly LanguageExt.HashSet<Key> subjects = [];
        public readonly LanguageExt.HashSet<SubjectSet> SubjectSets = [];

        public static SubjectMap Empty = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SubjectMap() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SubjectMap(
            LanguageExt.HashSet<Key> subjects,
            LanguageExt.HashSet<SubjectSet> subjectSets)
        {
            this.subjects = subjects;
            SubjectSets = subjectSets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SubjectMap Include(Either<Subject, SubjectSet> e) =>
            e.Match(
                Left: subject => new SubjectMap(subjects.AddOrUpdate(subject.AsKey()), SubjectSets),
                Right: subjectSet => new SubjectMap(subjects, SubjectSets.AddOrUpdate(subjectSet)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAMemberOf(Subject subject) =>
            subjects.Contains(subject.AsKey());
    }

    /// <summary>
    /// key = subjectSet as key
    /// Subjects = users included in the subjectSet
    /// </summary>
    private readonly Map<Key, SubjectMap> index = [];

    public AclStore() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AclStore(Map<Key, SubjectMap> acls) => index = acls;

    /// <summary>
    /// Checks for direct match or recusively scans the userset rewrite list.
    /// </summary>
    /// <param name="subject"></param>
    /// <param name="subjectSet"></param>
    /// <returns></returns>
    /// <remarks>
    /// CHECK(U,⟨object#relation⟩) =
    ///     ∃ tuple ⟨object#relation@U⟩
    ///     ∨ ∃tuple ⟨object#relation@U′⟩, where
    ///     U′ =⟨object′#relation′⟩ s.t. CHECK(U,U′).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAMemberOf(Subject subject, SubjectSet subjectSet, NamespaceTree tree) =>
        // todo: instead of passing namespace, look it up from the subjectset resource
        EvaluateRewrite(subject, subjectSet, tree, tree.Relationships[subjectSet.Relationship]);

    private bool EvaluateRewrite(Subject subject, SubjectSet subjectSet, NamespaceTree namespaceTree, SubjectSetRewrite node)
        => node switch
        {
            This =>
                ReadSubjectMap(subjectSet.AsKey()).IsAMemberOf(subject),

            ComputedSubjectSetRewrite computedSet =>
                IsAMemberOf(subject, new SubjectSet(subjectSet.Resource, computedSet.Relationship), namespaceTree),

            UnionRewrite union =>
                union.Children.Any(child => EvaluateRewrite(subject, subjectSet, namespaceTree, child)),

            IntersectionRewrite intersection =>
                intersection.Children.All(child => EvaluateRewrite(subject, subjectSet, namespaceTree, child)),

            ExclusionRewrite exclusion =>
                EvaluateRewrite(subject, subjectSet, namespaceTree, exclusion.Include)
            && !EvaluateRewrite(subject, subjectSet, namespaceTree, exclusion.Exclude),

            TupleToSubjectSetRewrite tupleToSubjectSet =>
                ReadSubjectMap(new SubjectSet(subjectSet.Resource, tupleToSubjectSet.TuplesetRelation).AsKey())
                    .SubjectSets
                    .Any(parentSubjectSet =>
                        IsAMemberOf(subject, new SubjectSet(parentSubjectSet.Resource, tupleToSubjectSet.ComputedSubjectSetRelation), namespaceTree)),

            _ => throw new NotSupportedException()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AclStore Include(Resource resource, Relationship relationship, Either<Subject, SubjectSet> subject) =>
        Include(new SubjectSet(resource, relationship), subject);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AclStore Include(SubjectSet subjectSet, Either<Subject, SubjectSet> subject) =>
        Include(subjectSet.AsKey(), subject);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AclStore Include(Key key, Either<Subject, SubjectSet> subject) =>
        new(index.AddOrUpdate(key, ReadSubjectMap(key).Include(subject)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SubjectMap ReadSubjectMap(Key key) =>
        index.Find(key).Match(
            Some: e => e,
            None: () => SubjectMap.Empty);
}
