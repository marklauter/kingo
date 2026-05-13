using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Extensions;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Kingo.Pdl.Tests.Architecture;

// Encodes the structural invariants that the writing-csharp first-slice pattern locks in
// for Kingo.Pdl. Drift trips the build instead of a human reviewer.
public sealed class ArchitectureTests
{
    private static readonly ArchitectureModel Policies = new ArchLoader()
        .LoadAssemblies(typeof(PdlParser).Assembly)
        .Build();

    [Fact]
    public void AllTypesResideInKingoPdlNamespace() =>
        Verify(Types()
            .That()
            .DoNotHaveNameContaining("<") // exclude compiler-generated closures / async state machines
            .Should()
            .ResideInNamespaceMatching(@"^Kingo\.Pdl(\..*)?$")
            .Because("Kingo.Pdl is a self-contained parser; types belong inside its namespace."));

    [Fact]
    public void ConcreteClassesAreSealed() =>
        Verify(Classes()
            .That()
            .AreNotAbstract() // C# 'static' compiles to 'abstract sealed' — this also excludes static factories
            .And()
            .DoNotHaveNameContaining("<")
            .Should()
            .BeSealed()
            .Because("writing-csharp: seal concrete classes by default — enables devirtualization, signals 'not for inheritance'."));

    [Fact]
    public void InstanceFieldsAreNotPublic() =>
        Verify(FieldMembers()
            .That()
            .AreNotStatic() // const / static readonly may be public; instance state must not be
            .And()
            .DoNotHaveNameContaining("<") // exclude compiler-generated backing fields
            .And()
            .DoNotHaveName("value__") // every CLR enum carries a synthesized public 'value__' instance field; nothing we can do about it
            .Should()
            .NotBePublic()
            .Because("writing-csharp: immutable by default — no public mutable instance state."));

    private static void Verify(IArchRule rule)
    {
        if (!rule.HasNoViolations(Policies))
            Assert.Fail(rule.Evaluate(Policies).ToErrorMessage());
    }
}
