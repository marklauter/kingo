using Results;

namespace Kingo.Graphs.Tests;

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
        Assert.Equal("relationship_path.invalid", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_DotsMarkerAsRelationship_IsRefused_TheDotsAreStorageOnlyVocabularyNotARelationship()
    {
        // '...' is not a relationship — it is the '#...' marker of the ResourceFact member production, fact-grammar
        // punctuation and storage-only vocabulary. As a relationship it fails the identifier grammar, so a subjectset
        // question spelled 'folder:y#...' dies at the parse edge (supersedes the F18 condition-2-by-contract test).
        var result = SubjectSet.Parse("folder:y#...");

        var failure = Assert.IsType<Result<SubjectSet>.Failure>(result);
        Assert.Equal("relationship_path.invalid", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_BothSidesInvalid_AccumulatesResourceThenRelationshipInvalid()
    {
        var result = SubjectSet.Parse("doc:a:b#mem-ber");

        var failure = Assert.IsType<Result<SubjectSet>.Failure>(result);
        Assert.Equal(["resource_id.invalid", "relationship_path.invalid"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_BothSidesEmpty_AccumulatesResourceEmptyThenRelationshipEmpty()
    {
        // left "" short-circuits to Resource.Parse's single "resource.empty"; right "" → "relationship_path.empty"
        var result = SubjectSet.Parse("#");

        var failure = Assert.IsType<Result<SubjectSet>.Failure>(result);
        Assert.Equal(["resource.empty", "relationship_path.empty"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_EqualInputs_ProduceEqualValues()
    {
        var left = Assert.IsType<Result<SubjectSet>.Success>(SubjectSet.Parse("doc:readme#viewer")).Value;
        var right = new SubjectSet(
            new Resource(NamespacePath.Unchecked("doc"), ResourceId.Unchecked("readme")),
            RelationshipPath.Unchecked("viewer"));

        Assert.Equal(right, left);
    }

    [Fact]
    public void Parse_DifferentRelationship_ProducesUnequalValues()
    {
        var left = Assert.IsType<Result<SubjectSet>.Success>(SubjectSet.Parse("doc:readme#viewer")).Value;
        var right = Assert.IsType<Result<SubjectSet>.Success>(SubjectSet.Parse("doc:readme#editor")).Value;

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Parse_Valid_RoundTripsThroughToString()
    {
        var success = Assert.IsType<Result<SubjectSet>.Success>(SubjectSet.Parse("doc:readme#viewer"));

        Assert.Equal("doc:readme#viewer", success.Value.ToString());
    }
}
