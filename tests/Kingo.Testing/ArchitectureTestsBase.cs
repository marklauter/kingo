using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Extensions;
using ArchUnitNET.Loader;
using System.Reflection;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Kingo.Testing;

public abstract class ArchitectureTestsBase(Assembly targetAssembly, string expectedNamespacePattern)
{
    private readonly ArchitectureModel architecture = new ArchLoader().LoadAssemblies(targetAssembly).Build();
    private readonly string namespacePattern = expectedNamespacePattern;

    [Fact]
    public void AllTypesResideInExpectedNamespace() =>
        Verify(Types()
            .That()
            .DoNotHaveNameContaining("<")
            .Should()
            .ResideInNamespaceMatching(namespacePattern)
            .Because($"types belong inside the project's namespace ({namespacePattern}).")
            .WithoutRequiringPositiveResults());

    [Fact]
    public void ConcreteClassesAreSealed() =>
        Verify(Classes()
            .That()
            .AreNotAbstract()
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
            .AreNotStatic()
            .And()
            .DoNotHaveNameContaining("<")
            .And()
            .DoNotHaveName("value__")
            .Should()
            .NotBePublic()
            .Because("writing-csharp: immutable by default — no public mutable instance state.")
            .WithoutRequiringPositiveResults());

    [Fact]
    public void ValueWrappersAreReadonlyRecordStructs()
    {
        var violations = targetAssembly.GetTypes()
            .Where(ImplementsIValueType)
            .Where(type => !IsReadonlyRecordStruct(type))
            .Select(type => type.FullName)
            .ToList();

        if (violations.Count > 0)
            Assert.Fail($"writing-csharp: IValueType<TSelf, TValue> implementors must be readonly record structs. Violations: {string.Join(", ", violations)}");
    }

    private static bool ImplementsIValueType(System.Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == "ValueTypes.IValueType`2");

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
