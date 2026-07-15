using Kingo.Graphs;
using Results;

namespace Kingo.Tests.Graphs;

public sealed class SubjectTests
{
    [Fact]
    public void Parse_DirectSubjectWithColon_SucceedsAndRoundTrips()
    {
        var result = Subject.Parse("user:anne");

        var success = Assert.IsType<Result<Subject>.Success>(result);
        var direct = Assert.IsType<DirectSubject>(success.Value);
        Assert.Equal("user:anne", direct.Id.Value);
        Assert.Equal("user:anne", direct.ToString());
    }

    [Fact]
    public void Parse_HashDispatchesToSubjectSet_SucceedsAndRoundTrips()
    {
        var result = Subject.Parse("team:sales#member");

        var success = Assert.IsType<Result<Subject>.Success>(result);
        var set = Assert.IsType<SubjectSet>(success.Value);
        Assert.Equal("team:sales", set.Resource.ToString());
        Assert.Equal("member", set.Relationship.Value);
        Assert.Equal("team:sales#member", set.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ReturnsSingleEmptyError(string input)
    {
        var result = Subject.Parse(input);

        var failure = Assert.IsType<Result<Subject>.Failure>(result);
        Assert.Equal("subject.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_InvalidDirectSubject_ReturnsSubjectIdInvalidError()
    {
        var result = Subject.Parse("an@ne");

        var failure = Assert.IsType<Result<Subject>.Failure>(result);
        Assert.Equal("subject_id.invalid", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void DirectSubject_EqualInputs_AreEqual()
    {
        var left = Assert.IsType<DirectSubject>(Assert.IsType<Result<Subject>.Success>(Subject.Parse("user:anne")).Value);
        var right = new DirectSubject(SubjectIdentifier.Create("user:anne"));

        Assert.Equal(right, left);
    }

    [Fact]
    public void DirectSubject_NeverEqualsSubjectSet()
    {
        var direct = Assert.IsType<Result<Subject>.Success>(Subject.Parse("user:anne")).Value;
        var set = Assert.IsType<Result<Subject>.Success>(Subject.Parse("team:sales#member")).Value;

        Assert.NotEqual(direct, set);
    }
}

public sealed class SubjectSetTests
{
    [Fact]
    public void Parse_Valid_PopulatesResourceAndRelationship()
    {
        var result = SubjectSet.Parse("doc:readme#viewer");

        var success = Assert.IsType<Result<SubjectSet>.Success>(result);
        Assert.Equal("doc:readme", success.Value.Resource.ToString());
        Assert.Equal("viewer", success.Value.Relationship.Value);
    }

    [Fact]
    public void Parse_MixedCaseRelationship_LowercasesRelationship()
    {
        var result = SubjectSet.Parse("doc:readme#VIEWER");

        var success = Assert.IsType<Result<SubjectSet>.Success>(result);
        Assert.Equal("viewer", success.Value.Relationship.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ReturnsSingleEmptyError(string input)
    {
        var result = SubjectSet.Parse(input);

        var failure = Assert.IsType<Result<SubjectSet>.Failure>(result);
        Assert.Equal("subjectset.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_NoSeparator_ReturnsSingleFormatError()
    {
        var result = SubjectSet.Parse("doc:readme");

        var failure = Assert.IsType<Result<SubjectSet>.Failure>(result);
        Assert.Equal("subjectset.format", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_SecondHashInRelationship_ReturnsSingleRelationshipInvalidError()
    {
        // split at the FIRST '#' leaves "member#extra" as the relationship, which rejects '#'
        var result = SubjectSet.Parse("team:sales#member#extra");

        var failure = Assert.IsType<Result<SubjectSet>.Failure>(result);
        Assert.Equal("relationship_id.invalid", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_BothSidesInvalid_AccumulatesResourceThenRelationshipInvalid()
    {
        var result = SubjectSet.Parse("doc:a:b#mem-ber");

        var failure = Assert.IsType<Result<SubjectSet>.Failure>(result);
        Assert.Equal(["resource_id.invalid", "relationship_id.invalid"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_BothSidesEmpty_AccumulatesResourceEmptyThenRelationshipEmpty()
    {
        // left "" short-circuits to Resource.Parse's single "resource.empty"; right "" → "relationship_id.empty"
        var result = SubjectSet.Parse("#");

        var failure = Assert.IsType<Result<SubjectSet>.Failure>(result);
        Assert.Equal(["resource.empty", "relationship_id.empty"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_EqualInputs_ProduceEqualValues()
    {
        var left = Assert.IsType<Result<SubjectSet>.Success>(SubjectSet.Parse("doc:readme#viewer")).Value;
        var right = new SubjectSet(
            new Resource(NamespaceIdentifier.Create("doc"), ResourceIdentifier.Create("readme")),
            RelationshipIdentifier.Create("viewer"));

        Assert.Equal(right, left);
    }
}
