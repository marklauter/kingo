using Kingo.DictionaryEncoding;
using Kingo.Namespaces;
using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;
using System.Runtime.CompilerServices;

namespace Kingo.Acl;

// PDP - policy decision point

public sealed class AclReader(
    DocumentReader<BigId, BigId> documentReader,
    RewriteReader rewriteReader,
    KeyEncoder encoder)
{

    // todo: consider returning a Decision response 
    // decision
    //   allowed | denied
    //   time
    //   subject
    //   subjectset
    //   other???
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Either<Error, bool> IsAMemberOf(Subject subject, SubjectSet subjectSet, CancellationToken ct) =>
        rewriteReader
            .Read(subjectSet.Resource.Namespace, subjectSet.Relationship)
            .Match(
                Some: rewrite => EvaluateRewrite(subject, subjectSet, rewrite, ct),
                None: () => false);

    private Either<Error, bool> EvaluateRewrite(Subject subject, SubjectSet subjectSet, SubjectSetRewrite node, CancellationToken ct)
    {
        static Either<Error, bool> Any(
            IEnumerable<SubjectSetRewrite> children,
            Func<SubjectSetRewrite, Either<Error, bool>> f) =>
            children.Select(f).Aggregate(
                Prelude.Right<Error, bool>(false),
                (acc, next) => acc.Bind(a => next.Map(n => a || n)));

        static Either<Error, bool> All(
            IEnumerable<SubjectSetRewrite> children,
            Func<SubjectSetRewrite, Either<Error, bool>> f) =>
            children.Select(f).Aggregate(
                Prelude.Right<Error, bool>(true),
                (acc, next) => acc.Bind(a => next.Map(n => a && n)));

        return node switch
        {
            This => encoder.Pack(subjectSet, ct)
                .Map(BigId.From)
                .Map(hk => documentReader.Find<Subject>(hk, subject.Id).IsSome),

            ComputedSubjectSetRewrite computedSet =>
                IsAMemberOf(subject, new SubjectSet(subjectSet.Resource, computedSet.Relationship), ct),

            UnionRewrite union =>
                Any(union.Children, child => EvaluateRewrite(subject, subjectSet, child, ct)),

            IntersectionRewrite intersection =>
                All(intersection.Children, child => EvaluateRewrite(subject, subjectSet, child, ct)),

            ExclusionRewrite exclusion =>
                Prelude.Right<Error, Func<bool, bool, bool>>((included, excluded) => included && !excluded)
                    .Apply(EvaluateRewrite(subject, subjectSet, exclusion.Include, ct))
                    .Apply(EvaluateRewrite(subject, subjectSet, exclusion.Exclude, ct)),

            TupleToSubjectSetRewrite tupleToSubjectSet =>
                encoder.Pack(subjectSet.Resource, tupleToSubjectSet.TuplesetRelation, ct)
                .Map(BigId.From)
                .Bind(hk => documentReader
                    .Find<SubjectSet>(hk, RangeKey.Unbound)
                    .Map(parentSubjectSet =>
                        IsAMemberOf(subject, new SubjectSet(parentSubjectSet.Record.Resource, tupleToSubjectSet.ComputedSubjectSetRelation), ct))
                    .Aggregate(
                        Prelude.Right<Error, bool>(false),
                        (acc, next) => acc.Bind(a => next.Map(n => a || n)))),

            TupleToSubjectSetRewrite tupleToSubjectSet =>
                encoder.Pack(subjectSet.Resource, tupleToSubjectSet.TuplesetRelation, ct)
                .Map(BigId.From)
                .Bind(hk => documentReader
                    .Find<SubjectSet>(hk, RangeKey.Unbound)
                    .Any(parentSubjectSet =>
                        (bool)IsAMemberOf(subject, new SubjectSet(parentSubjectSet.Record.Resource, tupleToSubjectSet.ComputedSubjectSetRelation), ct))
                    ,

            _ => throw new NotSupportedException($"node type {node.GetType()} not supported")
        };
    }
}
