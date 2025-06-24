using Kingo.Facts;
using System.Collections.Concurrent;

namespace Kingo.Storage;

public sealed class InMemoryAssociationStore
{
    // namespace:resource#relation@user
    private readonly ConcurrentDictionary<string, bool> acls = [];

    public bool Allow(Association association) =>
        acls.TryAdd(association.ToString(), true);

    public bool Deny(Association association) =>
        acls.TryAdd(association.ToString(), false);

    /*
    CHECK(U,⟨object#relation⟩) =
     ∃ tuple ⟨object#relation@U⟩
     ∨∃tuple ⟨object#relation@U′⟩, where
     U′ =⟨object′#relation′⟩ s.t. CHECK(U,U′).
    */
    // todo: work out the SubjectSet rewrite recursion
    public bool Check(Subject subject, SubjectSet set)
    {
        if (acls.TryGetValue(new Association(set.Resource, set.Relationship, subject).ToString(),
            out var allowed))
        {
            return allowed;
        }
        else
        {
            Check(subject, );
        }

    }
}
