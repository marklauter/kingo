using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Namespaces;

public sealed class RewriteReader(DocumentStore store)
{
    public Option<SubjectSetRewrite> Read(Namespace @namespace, Relationship relationship) =>
        store
        .Find<SubjectSetRewrite>(Key.From($"{nameof(Namespace)}/{@namespace}"), Key.From(relationship))
        .Map(d => d.Record);
}
