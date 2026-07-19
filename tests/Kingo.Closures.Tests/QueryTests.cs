using Kingo.Graphs;
using Results;
using static Kingo.Graphs.Subject;

namespace Kingo.Closures.Tests;

public sealed class QueryTests
{
    [Fact]
    public void Parse_DirectSubject_SucceedsAndRoundTrips()
    {
        var result = Query.Parse("doc:readme#viewer@user:anne");

        var success = Assert.IsType<Result<Query>.Success>(result);
        Assert.Equal("doc:readme", success.Value.SubjectSet.Resource.ToString());
        Assert.Equal("viewer", success.Value.SubjectSet.Relationship.Value);
        Assert.Equal("user:anne", success.Value.Subject.Id.Value);
        Assert.Equal("doc:readme#viewer@user:anne", success.Value.ToString());
    }

    [Fact]
    public void Parse_SubjectSetSubject_ReturnsSingleQuerySubjectError()
    {
        // legal as a Fact, unrepresentable as a Query: a query's subject is always direct
        var result = Query.Parse("doc:readme#viewer@team:sales#member");

        var failure = Assert.IsType<Result<Query>.Failure>(result);
        Assert.Equal("query.subject", Assert.Single(failure.Errors).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ReturnsSingleEmptyError(string input)
    {
        var result = Query.Parse(input);

        var failure = Assert.IsType<Result<Query>.Failure>(result);
        Assert.Equal("query.empty", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_NoSeparator_ReturnsSingleFormatError()
    {
        var result = Query.Parse("doc:readme#viewer");

        var failure = Assert.IsType<Result<Query>.Failure>(result);
        Assert.Equal("query.format", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_BothSidesInvalid_AccumulatesRelationshipThenSubjectIdInvalid()
    {
        // split at the FIRST '@' leaves "an@ne" as the subject; no '#', so SubjectIdentifier rejects '@'
        var result = Query.Parse("doc:x#vie-wer@an@ne");

        var failure = Assert.IsType<Result<Query>.Failure>(result);
        Assert.Equal(["relationship_id.invalid", "subject_id.invalid"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_BadSetSideAndSubjectSetSubject_AccumulatesRelationshipThenQuerySubject()
    {
        var result = Query.Parse("doc:x#vie-wer@team:sales#member");

        var failure = Assert.IsType<Result<Query>.Failure>(result);
        Assert.Equal(["relationship_id.invalid", "query.subject"], failure.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Parse_MixedCase_CanonicalizesThroughToString()
    {
        var query = Assert.IsType<Result<Query>.Success>(Query.Parse("DOC:x#VIEWER@user:anne")).Value;

        Assert.Equal("doc:x#viewer@user:anne", query.ToString());
    }

    [Fact]
    public void Parse_EqualInputs_ProduceEqualValues()
    {
        var left = Assert.IsType<Result<Query>.Success>(Query.Parse("doc:readme#viewer@user:anne")).Value;
        var right = new Query(
            new SubjectSet(
                new Resource(NamespaceIdentifier.Create("doc"), ResourceIdentifier.Create("readme")),
                RelationshipIdentifier.Create("viewer")),
            new DirectSubject(SubjectIdentifier.Create("user:anne")));

        Assert.Equal(right, left);
    }
}
