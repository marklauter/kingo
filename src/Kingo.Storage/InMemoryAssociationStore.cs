using Kingo.Facts;
using LanguageExt;
using System.Collections.Concurrent;

namespace Kingo.Storage;

public sealed class InMemoryAssociationStore
{
    // todo: AclTarget is not even a little bit safe for concurrent reads and writes
    private sealed class AclTarget
    {
        private readonly ConcurrentDictionary<string, bool> subjects = new();

        public readonly System.Collections.Generic.HashSet<SubjectSet> AllowedSubjectSets = [];
        public readonly System.Collections.Generic.HashSet<SubjectSet> DeniedSubjectSets = [];

        public bool Allow(Either<Subject, SubjectSet> subject) =>
            subject.Match(
                Left: sub => subjects.TryAdd(sub.AsKey(), true),
                Right: AllowedSubjectSets.Add);

        public bool Deny(Either<Subject, SubjectSet> subject) =>
            subject.Match(
                Left: sub => subjects.TryAdd(sub.AsKey(), false),
                Right: DeniedSubjectSets.Add);

        /// <summary>
        /// Returns true if a direct match in subjects is found, 
        /// otherwise returns the AclTarget for further processing by the association store.
        /// </summary>
        /// <param name="subject"></param>
        /// <returns><see cref="Either{AclTarget, bool}"/></returns>
        public Either<AclTarget, bool> IsAllowed(Subject subject) =>
            subjects.TryGetValue(subject.AsKey(), out var allowed)
                ? allowed
                : this;
    }

    private readonly ConcurrentDictionary<string, AclTarget> acls = [];

    public bool Allow(Association association) =>
        ReadAclTarget(association.ResourceRelationship)
        .Allow(association.Subject);

    public bool Deny(Association association) =>
        ReadAclTarget(association.ResourceRelationship)
        .Deny(association.Subject);

    private AclTarget ReadAclTarget(SubjectSet set) =>
        acls.GetOrAdd(set.ToString(), new AclTarget());

    /*
    CHECK(U,⟨object#relation⟩) =
        ∃ tuple ⟨object#relation@U⟩
        ∨∃tuple ⟨object#relation@U′⟩, where
        U′ =⟨object′#relation′⟩ s.t. CHECK(U,U′).
    */

    public bool Check(Subject subject, SubjectSet subjectSet) =>
        ReadAclTarget(subjectSet)
        .IsAllowed(subject)
        .Match(
            Left: target =>
                !target.DeniedSubjectSets.Any(set => Check(subject, set))
                && target.AllowedSubjectSets.Any(set => Check(subject, set)),
            Right: allowed => allowed);
}
