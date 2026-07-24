namespace Kingo.Documents;

/// <summary>The reserved keys of the theory document envelope, shared by the parse (<see cref="TheoryParser"/>) and print (<see cref="TheoryPrinter"/>) halves so the two never disagree on the wire format.</summary>
internal static class DocumentKeys
{
    public const string Name = "theory";
    public const string Namespaces = "namespaces";
}
