using Results;
using static Kingo.Graphs.Subject;

namespace Kingo.Graphs.Tests;

public sealed class FactTests
{
    [Fact]
    public void Parse_DirectSubject_SucceedsAndRoundTrips()
    {
        var result = Fact.Parse("doc:readme#viewer@user:anne");

        var success = Assert.IsType<Result<Fact>.Success>(result);
        Assert.Equal("doc:readme", success.Value.SubjectSet.Resource.ToString());
        Assert.Equal("viewer", success.Value.SubjectSet.Relationship.Value);
        var direct = Assert.IsType<DirectSubject>(success.Value.Subject);
        Assert.Equal("user:anne", direct.Id.Value);
        Assert.Equal("doc:readme#viewer@user:anne", success.Value.ToString());
    }

    [Fact]
    public void Parse_SubjectSetSubject_SucceedsAndRoundTrips()
    {
        var result = Fact.Parse("doc:readme#viewer@team:sales#member");

        var success = Assert.IsType<Result<Fact>.Success>(result);
        _ = Assert.IsType<SubjectSet>(success.Value.Subject);
        Assert.Equal("doc:readme#viewer@team:sales#member", success.Value.ToString());
    }

    [Fact]
    public void Parse_SubjectSet_IsTheStatementsLeftHandSide()
    {
        var fact = Assert.IsType<Result<Fact>.Success>(Fact.Parse("doc:readme#viewer@user:anne")).Value;

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
        // split at the FIRST '@' leaves "a@b" as the subject; no '#', so SubjectIdentifier rejects '@'
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
        var fact = Assert.IsType<Result<Fact>.Success>(Fact.Parse("DOC:x#VIEWER@user:anne")).Value;

        Assert.Equal("doc:x#viewer@user:anne", fact.ToString());
    }

    [Fact]
    public void Parse_EqualInputs_ProduceEqualValues()
    {
        var left = Assert.IsType<Result<Fact>.Success>(Fact.Parse("doc:readme#viewer@user:anne")).Value;
        var right = new Fact(
            new SubjectSet(
                new Resource(NamespaceIdentifier.Create("doc"), ResourceIdentifier.Create("readme")),
                RelationshipIdentifier.Create("viewer")),
            new DirectSubject(SubjectIdentifier.Create("user:anne")));

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
