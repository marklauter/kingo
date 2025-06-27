using Kingo.Facts;
using Kingo.Primitives;
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

    private readonly Map<Key, AclElements> acls = [];

    public AclStore() { }

    private AclStore(Map<Key, AclElements> acls) => this.acls = acls;

    public AclStore Union(Resource resource, Identifier relationship, Either<Subject, SubjectSet> e) =>
        Union(resource.AsKey(relationship), e);

    private AclStore Union(Key key, Either<Subject, SubjectSet> e) =>
        new(acls.AddOrUpdate(key, ReadAclElements(key).Union(e)));

    private AclElements ReadAclElements(Key key) =>
        acls.Find(key).Match(
            Some: e => e,
            None: () => new());

    /*
    CHECK(U,⟨object#relation⟩) =
        ∃ tuple ⟨object#relation@U⟩
        ∨∃tuple ⟨object#relation@U′⟩, where
        U′ =⟨object′#relation′⟩ s.t. CHECK(U,U′).
    */

    public bool IsAMemberOf(Subject subject, SubjectSet subjectSet) =>
        ReadAclElements(subjectSet.AsKey())
        .IsAMemberOf(subject)
        .Match(
            Left: isMember => isMember,
            Right: subjectSets => subjectSets.Any(subjectSet => IsAMemberOf(subject, subjectSet)));
}
