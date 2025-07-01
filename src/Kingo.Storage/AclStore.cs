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
    private sealed class Subjects
    {
        private readonly LanguageExt.HashSet<Key> subjects = [];
        public readonly LanguageExt.HashSet<SubjectSet> SubjectSets = [];

        public static Subjects Empty = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Subjects() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Subjects(
            LanguageExt.HashSet<Key> subjects,
            LanguageExt.HashSet<SubjectSet> subjectSets)
        {
            this.subjects = subjects;
            SubjectSets = subjectSets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Subjects Include(Either<Subject, SubjectSet> e) =>
            e.Match(
                Left: subject => new Subjects(subjects.AddOrUpdate(subject.AsKey()), SubjectSets),
                Right: subjectSet => new Subjects(subjects, SubjectSets.AddOrUpdate(subjectSet)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAMemberOf(Subject subject) =>
            subjects.Contains(subject.AsKey());
    }

    /// <summary>
    /// Key: the left side of a tuple. eg: resource and relation as key
    /// Subjects: the right side of a tuple. eg: subjects and subject sets
    /// </summary>
    private readonly Map<Key, Subjects> tuples = [];

    private AclStore() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AclStore(Map<Key, Subjects> acls) => tuples = acls;

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
        new(tuples.AddOrUpdate(key, ReadSubjectMap(key).Include(subject)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Subjects ReadSubjectMap(Key key) =>
        tuples.Find(key).Match(
            Some: e => e,
            None: () => Subjects.Empty);
}
