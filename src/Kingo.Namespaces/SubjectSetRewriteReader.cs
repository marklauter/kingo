using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Namespaces;

public sealed class SubjectSetRewriteReader(DocumentStore documentStore)
{
    public Option<SubjectSetRewrite> Read(Namespace @namespace, Relationship relationship) =>
        documentStore
        .Find<SubjectSetRewrite>(Key.From($"{nameof(Namespace)}/{@namespace}"), Key.From(relationship))
        .Map(d => d.Record);
}
