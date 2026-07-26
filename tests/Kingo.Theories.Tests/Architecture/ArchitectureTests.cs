using Kingo.Testing;
using System.Reflection;

namespace Kingo.Theories.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo.Theories"), @"^Kingo\.Theories(\..*)?$")
{
    /// <summary>
    /// The theory side and the fact side are independent halves of the domain — they meet only in the rewrite
    /// interpreter, which consumes both. Neither references the other; their only shared vocabulary is the identifiers
    /// in <c>Kingo</c>.
    /// </summary>
    [Fact]
    public void DoesNotDependOnFacts()
    {
        var violations = Assembly.Load("Kingo.Theories")
            .GetReferencedAssemblies()
            .Where(reference => reference.Name == "Kingo.Facts")
            .Select(reference => reference.FullName)
            .ToList();

        if (violations.Count > 0)
            Assert.Fail($"the theory side never depends on the fact side — the two meet in the rewrite interpreter, not in each other. Violations: {string.Join(", ", violations)}");
    }
}
