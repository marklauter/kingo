using Kingo.Testing;
using System.Reflection;

namespace Kingo.Facts.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo.Facts"), @"^Kingo\.Facts(\..*)?$")
{
    [Fact]
    public void DoesNotDependOnTheories()
    {
        var violations = Assembly.Load("Kingo.Facts")
            .GetReferencedAssemblies()
            .Where(reference => reference.Name == "Kingo.Theories")
            .Select(reference => reference.FullName)
            .ToList();

        if (violations.Count > 0)
            Assert.Fail($"the fact side never depends on the theory side — the two meet in the rewrite interpreter, not in each other. Violations: {string.Join(", ", violations)}");
    }
}
