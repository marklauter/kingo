using Results;

namespace Kingo.Graphs.Tests;

public sealed class SubjectTests
{
    [Fact]
    public void Parse_WithColon_SucceedsAndRoundTrips()
    {
        var result = Subject.Parse("user:anne");

        var success = Assert.IsType<Result<Subject>.Success>(result);
        Assert.Equal("user:anne", success.Value.Id.Value);
        Assert.Equal("user:anne", success.Value.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ReturnsSubjectIdEmptyError(string input)
    {
        var result = Subject.Parse(input);

        var failure = Assert.IsType<Result<Subject>.Failure>(result);
        Assert.Equal("subject_id.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_InvalidCharacters_ReturnsSubjectIdInvalidError()
    {
        var result = Subject.Parse("an@ne");

        var failure = Assert.IsType<Result<Subject>.Failure>(result);
        Assert.Equal("subject_id.invalid", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void EqualInputs_AreEqual()
    {
        var left = Assert.IsType<Result<Subject>.Success>(Subject.Parse("user:anne")).Value;
        var right = new Subject(SubjectIdentifier.Create("user:anne"));

        Assert.Equal(right, left);
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
