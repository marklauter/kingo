using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Kingo.Tests;

public sealed class RecordMapping
{
    private record R(
        [Required]
        string Name,
        [property: NotMapped]
        string Value);

    public static string[] PropertyNames { get; } = [.. typeof(R)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(pi => pi.CanRead)
        .Where(pi=> !pi.IsDefined(typeof(NotMappedAttribute), true))
        .Where(pi => pi.GetIndexParameters().Length == 0)
        //.Where(pi => !ExcludedNames.Contains(pi.Name))
        .Select(pi => pi.Name)];

    [Fact]
    public void Test()
    {
        _ = Assert.Single(PropertyNames);
        Assert.Contains("Name", PropertyNames);
        Assert.DoesNotContain("Value", PropertyNames);
    }
}
