using System.Reflection;

namespace Kingo.Testing;

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
