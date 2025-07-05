using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Namespaces;

public sealed class RewriteReader(DocumentReader reader)
{
    public Option<SubjectSetRewrite> Read(Namespace @namespace, Relationship relationship) =>
        reader
        .Find<SubjectSetRewrite>(
            hashKey: $"{nameof(Namespace)}/{@namespace}",
            rangeKey: Key.From(relationship))
        .Map(d => d.Record);
}
