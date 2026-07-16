using Kingo.Graphs;
using Results;

namespace Kingo.Sdl;

/// <summary>
/// STUB — the fact-side document adapter: graph document text to the operations it carries. Unlike <see cref="SchemaParser"/> this has no printer
/// half and no round-trip law: the document is a <b>bulk DML changeset</b> — a list of create/touch/delete operations — not a serialized state, and
/// a changeset is not something a graph can be printed back to (docs/notes/graph-document-is-bulk-dml.md).
/// <para>
/// The signature below is therefore <b>known wrong</b> and kept only until that proposal settles: <c>Result&lt;Graph&gt;</c> denotes a state, where a
/// changeset wants <c>Result&lt;ImmutableArray&lt;FactOperation&gt;&gt;</c> over a <c>Create | Touch | Delete</c> DU. Two open questions block the
/// restub — whether deleting an absent fact is a no-op or a failure, and whether a document is an ordered transaction — because both change the type.
/// </para>
/// <para>
/// The tuple grammar itself stays core (<see cref="Fact.Parse"/> owns <c>&lt;resource&gt;#&lt;relationship&gt;@&lt;subject&gt;</c> — docs/notes/domain-language.md);
/// this adapter owns only the YAML envelope around it, the same division <see cref="SchemaParser"/> keeps.
/// </para>
/// </summary>
public static class GraphParser
{
    /// <summary>
    /// STUB — parses untrusted graph document text, returning every accumulated validation <see cref="Error"/> or the operations it carries.
    /// The return type is provisional; see the type-level remarks.
    /// </summary>
    public static Result<Graph> Parse(string text) => throw new NotImplementedException();
}
