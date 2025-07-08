using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Namespaces;

public sealed class RewriteReader(DocumentReader<Key, Key> reader)
{
    private static readonly Key RewriteValueKey = Key.From("ssr");

    public Option<SubjectSetRewrite> Read(Namespace @namespace, Relationship relationship) =>
        reader
        .Find($"{nameof(Namespace)}/{@namespace}", Key.From(relationship))
        .Map(d => d[RewriteValueKey])
        .Map(o => (SubjectSetRewrite)o);
}
