using System.Reflection;

namespace Kingo.Testing;

/// <summary>
/// Architecture rules for serialization-adapter projects, layered on the universal <see cref="ArchitectureTestsBase"/> rules. One invariant defines the adapter
/// layer today (docs/notes/architecture.md): an adapter defines no exception types, because parse failures at the trust boundary are <c>Result</c> values and
/// substrate faults propagate as the substrate's own exception types. The former public-surface rule ("every public type implements a port") died with the
/// <c>IDocumentSerializer</c> port; its replacement convention is pending (docs/notes/realign-serialization-projects-around-their-real-consumers.md).
/// </summary>
public abstract class AdapterArchitectureTestsBase(Assembly targetAssembly, string expectedNamespacePattern)
    : ArchitectureTestsBase(targetAssembly, expectedNamespacePattern)
{
    private readonly Assembly assembly = targetAssembly;

    [Fact]
    public void NoExceptionTypesAreDefined()
    {
        var violations = assembly.GetTypes()
            .Where(type => typeof(Exception).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .ToList();

        if (violations.Count > 0)
            Assert.Fail($"adapter layer: parse failures surface as Result values and substrate faults propagate as the substrate's own exception types — an adapter never mints its own. Violations: {string.Join(", ", violations)}");
    }
}
