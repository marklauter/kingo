using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Extensions;
using ArchUnitNET.Loader;
using System.Reflection;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Kingo.Testing;

/// <summary>
/// Base class for per-project architecture tests. Derive once per test project, pass the system-under-test assembly and the regex that types in that project must match. The three universal rules (namespace shape, sealed concrete classes, no public instance fields) inherit automatically.
/// </summary>
/// <remarks>
/// Use <see cref="Assembly.Load(string)"/> in the derived constructor — that lets empty assemblies (no domain types yet) still load without a marker type.
/// </remarks>
public abstract class ArchitectureTestsBase(Assembly targetAssembly, string expectedNamespacePattern)
{
    private readonly ArchitectureModel architecture = new ArchLoader().LoadAssemblies(targetAssembly).Build();
    private readonly string namespacePattern = expectedNamespacePattern;

    [Fact]
    public void AllTypesResideInExpectedNamespace() =>
        Verify(Types()
            .That()
            .DoNotHaveNameContaining("<") // exclude compiler-generated closures / async state machines
            .Should()
            .ResideInNamespaceMatching(namespacePattern)
            .Because($"types belong inside the project's namespace ({namespacePattern}).")
            .WithoutRequiringPositiveResults());

    [Fact]
    public void ConcreteClassesAreSealed() =>
        Verify(Classes()
            .That()
            .AreNotAbstract() // C# 'static' compiles to 'abstract sealed' — this also excludes static factories
            .And()
            .DoNotHaveNameContaining("<")
            .Should()
            .BeSealed()
            .Because("writing-csharp: seal concrete classes by default — enables devirtualization, signals 'not for inheritance'.")
            .WithoutRequiringPositiveResults());

    [Fact]
    public void InstanceFieldsAreNotPublic() =>
        Verify(FieldMembers()
            .That()
            .AreNotStatic() // const / static readonly may be public; instance state must not be
            .And()
            .DoNotHaveNameContaining("<") // exclude compiler-generated backing fields
            .And()
            .DoNotHaveName("value__") // every CLR enum carries a synthesized public 'value__' instance field
            .Should()
            .NotBePublic()
            .Because("writing-csharp: immutable by default — no public mutable instance state.")
            .WithoutRequiringPositiveResults());

    private void Verify(IArchRule rule)
    {
        if (!rule.HasNoViolations(architecture))
            Assert.Fail(rule.Evaluate(architecture).ToErrorMessage());
    }
}
