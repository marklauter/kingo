using Kingo.Graphs;

namespace Kingo.Sdl;

/// <summary>
/// STUB — prints a <see cref="Graph"/> to its graph document text, the serialize half of the graph round trip
/// (<see cref="GraphParser.Parse"/> is the other) and the fact-side counterpart to <see cref="SchemaPrinter"/>.
/// An extension so the call site reads as a domain capability (<c>graph.Print()</c>) while the format knowledge
/// stays in the adapter.
/// </summary>
public static class GraphPrinter
{
    /// <summary>STUB — emits the graph document for <paramref name="graph"/>, one fact per line in graph order.</summary>
    public static string Print(this Graph graph) => throw new NotImplementedException();
}
