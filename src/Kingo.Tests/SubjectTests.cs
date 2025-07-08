namespace Kingo.Tests;

public sealed class SubjectTests
{
    [Fact]
    public void Constructor_InitializesId()
    {
        var id = BigId.Zero;
        var subject = new Subject(id);
        Assert.Equal(id, subject.Id);
    }

    [Fact]
    public void Equality_ReturnsTrue_ForEqualInstances()
    {
        var id = BigId.Zero;
        var subject1 = new Subject(id);
        var subject2 = new Subject(id);

        Assert.True(subject1 == subject2);
        Assert.False(subject1 != subject2);
        Assert.True(subject1.Equals(subject2));
    }

    [Fact]
    public void Equality_ReturnsFalse_ForDifferentInstances()
    {
        var subject1 = new Subject(1);
        var subject2 = new Subject(2);

        Assert.False(subject1 == subject2);
        Assert.True(subject1 != subject2);
        Assert.False(subject1.Equals(subject2));
    }
}
