using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Acl;

public sealed class NamespaceReader(DocumentStore documentStore)
{
    public Option<SubjectSetRewrite> Find(Key hashKey, Key rangeKey) =>
        documentStore
        .Find<SubjectSetRewrite>(hashKey, rangeKey)
        .Map(d => d.Record);
}

