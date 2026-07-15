using Kingo.Statements;
using Results;

namespace Kingo.Tests.Statements;

public sealed class StatementTests
{
    [Fact]
    public void Parse_DirectSubject_SucceedsAndRoundTrips()
    {
        var result = Statement.Parse("doc:readme#viewer@user:anne");

        var success = Assert.IsType<Result<Statement>.Success>(result);
        Assert.Equal("doc:readme", success.Value.Resource.ToString());
        Assert.Equal("viewer", success.Value.Relationship.Value);
        var direct = Assert.IsType<DirectSubject>(success.Value.Subject);
        Assert.Equal("user:anne", direct.Id.Value);
        Assert.Equal("doc:readme#viewer@user:anne", success.Value.ToString());
    }

    [Fact]
    public void Parse_SubjectSetSubject_SucceedsAndRoundTrips()
    {
        var result = Statement.Parse("doc:readme#viewer@team:sales#member");

        var success = Assert.IsType<Result<Statement>.Success>(result);
        _ = Assert.IsType<SubjectSet>(success.Value.Subject);
        Assert.Equal("doc:readme#viewer@team:sales#member", success.Value.ToString());
    }

    [Fact]
    public void SubjectSet_ReflectsResourceAndRelationship()
    {
        var statement = Assert.IsType<Result<Statement>.Success>(Statement.Parse("doc:readme#viewer@user:anne")).Value;

        Assert.Equal(new SubjectSet(statement.Resource, statement.Relationship), statement.SubjectSet);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ReturnsSingleEmptyError(string input)
    {
        var result = Statement.Parse(input);

        var failure = Assert.IsType<Result<Statement>.Failure>(result);
        Assert.Equal("statement.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_NoSeparator_ReturnsSingleFormatError()
    {
        var result = Statement.Parse("doc:readme#viewer");

        var failure = Assert.IsType<Result<Statement>.Failure>(result);
        Assert.Equal("statement.format", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_SecondAtInSubject_ReturnsSingleSubjectIdInvalidError()
    {
        // split at the FIRST '@' leaves "a@b" as the subject; no '#', so SubjectIdentifier rejects '@'
        var result = Statement.Parse("doc:x#viewer@a@b");

        var failure = Assert.IsType<Result<Statement>.Failure>(result);
        Assert.Equal("subject_id.invalid", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_BothSidesInvalid_AccumulatesRelationshipThenSubjectIdInvalid()
    {
        var result = Statement.Parse("doc:x#vie-wer@an@ne");

        var failure = Assert.IsType<Result<Statement>.Failure>(result);
        Assert.Equal(["relationship_id.invalid", "subject_id.invalid"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_MixedCase_CanonicalizesThroughToString()
    {
        var statement = Assert.IsType<Result<Statement>.Success>(Statement.Parse("DOC:x#VIEWER@user:anne")).Value;

        Assert.Equal("doc:x#viewer@user:anne", statement.ToString());
    }

    [Fact]
    public void Parse_EqualInputs_ProduceEqualValues()
    {
        var left = Assert.IsType<Result<Statement>.Success>(Statement.Parse("doc:readme#viewer@user:anne")).Value;
        var right = new Statement(
            new Resource(NamespaceIdentifier.Create("doc"), ResourceIdentifier.Create("readme")),
            RelationshipIdentifier.Create("viewer"),
            new DirectSubject(SubjectIdentifier.Create("user:anne")));

        Assert.Equal(right, left);
    }

    [Fact]
    public void Parse_DifferentSubject_ProducesUnequalValues()
    {
        var left = Assert.IsType<Result<Statement>.Success>(Statement.Parse("doc:readme#viewer@user:anne")).Value;
        var right = Assert.IsType<Result<Statement>.Success>(Statement.Parse("doc:readme#viewer@team:sales#member")).Value;

        Assert.NotEqual(left, right);
    }
}
