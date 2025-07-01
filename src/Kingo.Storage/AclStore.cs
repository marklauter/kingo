using Kingo.Facts;
using LanguageExt;

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

        public Subjects() { }

        private Subjects(LanguageExt.HashSet<Key> subjects) =>
            this.subjects = subjects;

        public Subjects Include(Subject subject) =>
                 new(subjects.AddOrUpdate(subject.AsKey()));

        public bool IsAMemberOf(Subject subject) =>
            subjects.Contains(subject.AsKey());
    }

    /// <summary>
    /// key = subjectSet as key
    /// Subjects = users included in the subjectSet
    /// </summary>
    private readonly Map<Key, Subjects> index = [];

    public AclStore() { }

    private AclStore(Map<Key, Subjects> acls) => index = acls;

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

            _ => throw new NotSupportedException()
        };

    /// <summary>
    /// Binds a resource, relationship, subject tuple and adds it to a new AclStore.
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="relationship"></param>
    /// <param name="subject"></param>
    /// <returns>A new AclStore that is the union of the AclStore and the new tuple.</returns>
    public AclStore Include(Resource resource, Relationship relationship, Subject subject) =>
        Include(new SubjectSet(resource, relationship), subject);

    /// <summary>
    /// Binds a resource, relationship, subject tuple and adds it to a new AclStore.
    /// </summary>
    /// <param name="subjectSet"></param>
    /// <param name="subject"></param>
    /// <returns>A new AclStore that is the union of the AclStore and the new tuple.</returns>
    public AclStore Include(SubjectSet subjectSet, Subject subject) =>
        Include(subjectSet.AsKey(), subject);

    private AclStore Include(Key key, Subject subject) =>
        new(index.AddOrUpdate(key, ReadSubjectMap(key).Include(subject)));

    private Subjects ReadSubjectMap(Key key) =>
        index.Find(key).Match(
            Some: e => e,
            None: () => new());
}
