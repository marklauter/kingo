using Kingo.Testing;
using System.Reflection;

namespace Kingo.Schemas.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo.Schemas"), @"^Kingo\.Schemas(\..*)?$")
{
    /// <summary>
    /// The config model and the statement model are independent halves of the domain — they meet only in the rewrite
    /// interpreter, which consumes both. Neither references the other; their only shared vocabulary is the identifiers
    /// in <c>Kingo</c>.
    /// </summary>
    [Fact]
    public void DoesNotDependOnGraphs()
    {
        var violations = Assembly.Load("Kingo.Schemas")
            .GetReferencedAssemblies()
            .Where(reference => reference.Name == "Kingo.Graphs")
            .Select(reference => reference.FullName)
            .ToList();

        if (violations.Count > 0)
            Assert.Fail($"the config model never depends on the statement model — the two meet in the rewrite interpreter, not in each other. Violations: {string.Join(", ", violations)}");
    }
}
