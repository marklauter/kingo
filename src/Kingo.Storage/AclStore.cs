using Kingo.Facts;
using LanguageExt;

namespace Kingo.Storage;

// <summary>
/// This is a demo store. A production store would use DynamoDB, Casandra, or other versioned key-value store.
/// </summary>
public sealed class AclStore
{
    private sealed class AclElements
    {
        private readonly LanguageExt.HashSet<string> subjects = [];
        private readonly LanguageExt.HashSet<SubjectSet> subjectSets = [];

        public AclElements() { }

        private AclElements(
            LanguageExt.HashSet<string> subjects,
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

    private readonly Map<string, AclElements> acls = [];

    public AclStore() { }

    private AclStore(Map<string, AclElements> acls) => this.acls = acls;

    public AclStore Union(Resource resource, Relationship relationship, Either<Subject, SubjectSet> e) =>
        Union(resource.AsKey(relationship), e);

    private AclStore Union(string key, Either<Subject, SubjectSet> e) =>
        new(acls.AddOrUpdate(key, ReadAclElements(key).Union(e)));

    private AclElements ReadAclElements(string key) =>
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
