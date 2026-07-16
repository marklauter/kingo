using Kingo.Graphs;
using Results;

namespace Kingo.Sdl;

/// <summary>
/// STUB — the parse half of the graph adapter: graph document text to the core <see cref="Graph"/> model
/// (<see cref="GraphPrinter.Print"/> renders the other direction), the fact-side counterpart to
/// <see cref="SchemaParser"/>. The tuple grammar itself is core (<see cref="Fact.Parse"/> owns
/// <c>&lt;resource&gt;#&lt;relationship&gt;@&lt;subject&gt;</c> — docs/notes/domain-language.md); what this adapter
/// owns is the <i>document</i> around it, and that format is not yet chosen.
/// </summary>
public static class GraphParser
{
    /// <summary>
    /// STUB — parses untrusted graph document text, returning the defined <see cref="Graph"/> or every accumulated
    /// validation <see cref="Error"/>.
    /// </summary>
    public static Result<Graph> Parse(string text) => throw new NotImplementedException();
}
