using Kingo.Configuration.Spec;
using Kingo.Configuration.Tree;
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
    private sealed class AclElements
    {
        private readonly LanguageExt.HashSet<Key> subjects = [];
        private readonly LanguageExt.HashSet<SubjectSet> subjectSets = [];

        public AclElements() { }

        private AclElements(
            LanguageExt.HashSet<Key> subjects,
            LanguageExt.HashSet<SubjectSet> subjectSets)
        {
            this.subjects = subjects;
            this.subjectSets = subjectSets;
        }

        public AclElements Union(Either<Subject, SubjectSet> e) =>
            e.Match(
                Left: subject => new AclElements(subjects.Add(subject.AsKey()), subjectSets),
                Right: subjectSet => new AclElements(subjects, subjectSets.Add(subjectSet)));

        public Either<bool, LanguageExt.HashSet<SubjectSet>> IsAMemberOf(Subject subject) =>
            subjects.Contains(subject.AsKey()) ? true : subjectSets;
    }

    private readonly Map<Key, AclElements> aclIndex = [];

    public AclStore() { }

    private AclStore(Map<Key, AclElements> acls) => aclIndex = acls;

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

    private bool EvaluateRewrite(Subject subject, SubjectSet subjectSet, NamespaceTree namespaceTree, RewriteNode node)
        => node switch
        {
            ThisNode => ReadAclElements(subjectSet.AsKey())
                .IsAMemberOf(subject)
                .Match(
                    Left: isMember => isMember,
                    Right: subjectSets => subjectSets.Any(ss => IsAMemberOf(subject, ss, namespaceTree))
                ),
            ComputedSubjectSetNode computed =>
                IsAMemberOf(subject, new SubjectSet(subjectSet.Resource, computed.Relationship), namespaceTree),
            OperationNode op => op.Operation switch
            {
                SetOperation.Union => op.Children.Any(child => EvaluateRewrite(subject, subjectSet, namespaceTree, child)),
                SetOperation.Intersection => op.Children.All(child => EvaluateRewrite(subject, subjectSet, namespaceTree, child)),
                SetOperation.Exclusion => op.Children.Length == 2 &&
                    EvaluateRewrite(subject, subjectSet, namespaceTree, op.Children[0]) &&
                    !EvaluateRewrite(subject, subjectSet, namespaceTree, op.Children[1]),
                _ => throw new NotSupportedException()
            },
            _ => throw new NotSupportedException()
        };

    /// <summary>
    /// Binds a resource, relationship, subject tuple and adds it to a new AclStore.
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="relationship"></param>
    /// <param name="e"></param>
    /// <returns>A new AclStore that is the union of the AclStore and the new tuple.</returns>
    public AclStore Union(SubjectSet subjectSet, Either<Subject, SubjectSet> e) =>
        Union(subjectSet.AsKey(), e);

    private AclStore Union(Key key, Either<Subject, SubjectSet> e) =>
        new(aclIndex.AddOrUpdate(key, ReadAclElements(key).Union(e)));

    private AclElements ReadAclElements(Key key) =>
        aclIndex.Find(key).Match(
            Some: e => e,
            None: () => new());
}
