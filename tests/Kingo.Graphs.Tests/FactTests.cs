using Results;
using static Kingo.Graphs.Fact;

namespace Kingo.Graphs.Tests;

public sealed class FactTests
{
    [Fact]
    public void Parse_SubjectFact_SucceedsAndRoundTrips()
    {
        var result = Fact.Parse("doc:readme#viewer@user:anne");

        var success = Assert.IsType<Result<Fact>.Success>(result);
        var fact = Assert.IsType<SubjectFact>(success.Value);
        Assert.Equal("doc:readme", fact.SubjectSet.Resource.ToString());
        Assert.Equal("viewer", fact.SubjectSet.Relationship.Value);
        Assert.Equal("user:anne", fact.Subject.Id.Value);
        Assert.Equal("doc:readme#viewer@user:anne", fact.ToString());
    }

    [Fact]
    public void Parse_SubjectSetFact_SucceedsAndRoundTrips()
    {
        var result = Fact.Parse("doc:readme#viewer@team:sales#member");

        var success = Assert.IsType<Result<Fact>.Success>(result);
        var fact = Assert.IsType<SubjectSetFact>(success.Value);
        Assert.Equal("team:sales#member", fact.Subject.ToString());
        Assert.Equal("doc:readme#viewer@team:sales#member", fact.ToString());
    }

    [Fact]
    public void Parse_SubjectSet_IsTheLeftHandSide()
    {
        var success = Assert.IsType<Result<Fact>.Success>(Fact.Parse("doc:readme#viewer@user:anne"));
        var fact = Assert.IsType<SubjectFact>(success.Value);

        Assert.Equal(
            new SubjectSet(
                new Resource(NamespaceIdentifier.Create("doc"), ResourceIdentifier.Create("readme")),
                RelationshipIdentifier.Create("viewer")),
            fact.SubjectSet);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ReturnsSingleEmptyError(string input)
    {
        var result = Fact.Parse(input);

        var failure = Assert.IsType<Result<Fact>.Failure>(result);
        Assert.Equal("fact.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_NoSeparator_ReturnsSingleFormatError()
    {
        var result = Fact.Parse("doc:readme#viewer");

        var failure = Assert.IsType<Result<Fact>.Failure>(result);
        Assert.Equal("fact.format", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_SecondAtInSubject_ReturnsSingleSubjectIdInvalidError()
    {
        // split at the FIRST '@' leaves "a@b" as the subject; no '#', so Subject rejects '@'
        var result = Fact.Parse("doc:x#viewer@a@b");

        var failure = Assert.IsType<Result<Fact>.Failure>(result);
        Assert.Equal("subject_id.invalid", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_BothSidesInvalid_AccumulatesRelationshipThenSubjectIdInvalid()
    {
        var result = Fact.Parse("doc:x#vie-wer@an@ne");

        var failure = Assert.IsType<Result<Fact>.Failure>(result);
        Assert.Equal(["relationship_id.invalid", "subject_id.invalid"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_MixedCase_CanonicalizesThroughToString()
    {
        var success = Assert.IsType<Result<Fact>.Success>(Fact.Parse("DOC:x#VIEWER@user:anne"));

        Assert.Equal("doc:x#viewer@user:anne", success.Value.ToString());
    }

    [Fact]
    public void Parse_EqualInputs_ProduceEqualValues()
    {
        var left = Assert.IsType<Result<Fact>.Success>(Fact.Parse("doc:readme#viewer@user:anne")).Value;
        var right = new SubjectFact(
            new SubjectSet(
                new Resource(NamespaceIdentifier.Create("doc"), ResourceIdentifier.Create("readme")),
                RelationshipIdentifier.Create("viewer")),
            new Subject(SubjectIdentifier.Create("user:anne")));

        Assert.Equal(right, left);
    }

    [Fact]
    public void Parse_DifferentSubject_ProducesUnequalValues()
    {
        var left = Assert.IsType<Result<Fact>.Success>(Fact.Parse("doc:readme#viewer@user:anne")).Value;
        var right = Assert.IsType<Result<Fact>.Success>(Fact.Parse("doc:readme#viewer@team:sales#member")).Value;

        Assert.NotEqual(left, right);
    }
}
