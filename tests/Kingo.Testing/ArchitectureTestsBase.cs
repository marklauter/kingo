using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Extensions;
using ArchUnitNET.Loader;
using System.Reflection;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Kingo.Testing;

/// <summary>
/// Base class for per-project architecture tests. Derive once per test project, pass the system-under-test assembly and the regex that types in that project
/// must match. The universal rules (namespace shape, sealed concrete classes, no public instance fields, IValue implementors are readonly record structs)
/// inherit automatically.
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

    [Fact]
    public void ValueWrappersAreReadonlyRecordStructs()
    {
        // ArchUnitNET does not model readonly-ness or record synthesis, so this rule uses reflection:
        // readonly structs carry [IsReadOnly]; record structs synthesize a non-public PrintMembers(StringBuilder).
        var violations = targetAssembly.GetTypes()
            .Where(ImplementsIValue)
            .Where(type => !IsReadonlyRecordStruct(type))
            .Select(type => type.FullName)
            .ToList();

        if (violations.Count > 0)
            Assert.Fail($"writing-csharp: IValue<TSelf, TValue> implementors must be readonly record structs. Violations: {string.Join(", ", violations)}");
    }

    private static bool ImplementsIValue(System.Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == "Values.IValue`2");

    private static bool IsReadonlyRecordStruct(System.Type type) =>
        type.IsValueType
        && type.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), inherit: false)
        && type.GetMethod("PrintMembers", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(System.Text.StringBuilder)]) is not null;

    private void Verify(IArchRule rule)
    {
        if (!rule.HasNoViolations(architecture))
            Assert.Fail(rule.Evaluate(architecture).ToErrorMessage());
    }
}
