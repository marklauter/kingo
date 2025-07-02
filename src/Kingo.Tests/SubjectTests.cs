namespace Kingo.Tests;

public sealed class SubjectTests
{
    [Fact]
    public void Constructor_InitializesId()
    {
        var id = Guid.NewGuid();
        var subject = new Subject(id);
        Assert.Equal(id, subject.Id);
    }

    [Fact]
    public void Equality_ReturnsTrue_ForEqualInstances()
    {
        var id = Guid.NewGuid();
        var subject1 = new Subject(id);
        var subject2 = new Subject(id);

        Assert.True(subject1 == subject2);
        Assert.False(subject1 != subject2);
        Assert.True(subject1.Equals(subject2));
    }

    [Fact]
    public void Equality_ReturnsFalse_ForDifferentInstances()
    {
        var subject1 = new Subject(Guid.NewGuid());
        var subject2 = new Subject(Guid.NewGuid());

        Assert.False(subject1 == subject2);
        Assert.True(subject1 != subject2);
        Assert.False(subject1.Equals(subject2));
    }
}
