using LanguageExt;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Kingo.Policies;

internal sealed class RelationTypeConverter
    : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Relation);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        // Handles the simple case: "- owner"
        if (parser.TryConsume<Scalar>(out var scalar))
            return new Relation(RelationIdentifier.From(scalar.Value));

        // Handles the complex case: "- editor: this | owner"
        if (parser.TryConsume<MappingStart>(out _))
        {
            var name = parser.Consume<Scalar>().Value;
            var expression = parser.Consume<Scalar>().Value;
            _ = parser.Consume<MappingEnd>();

            var rewriteResult = RewriteExpressionParser.Parse(expression).Run();

            return rewriteResult.Match(
                Succ: rewrite => new Relation(RelationIdentifier.From(name), rewrite),
                Fail: err => throw new YamlException($"Failed to parse rewrite expression '{expression}': {err.Message}")
            );
        }

        throw new YamlException("Unexpected token type for Relation deserialization. Expected a scalar or a mapping.");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not Relation relation)
            throw new ArgumentException($"Expected Relation but got {value?.GetType()?.Name ?? "null"}");

        // For simple relations with ThisRewrite, emit as scalar: "- owner"
        if (relation.SubjectSetRewrite is ThisRewrite)
        {
            emitter.Emit(new Scalar(relation.Name.ToString()));
            return;
        }

        // For complex relations, emit as mapping: "- editor: this | owner"
        emitter.Emit(new MappingStart());
        emitter.Emit(new Scalar(relation.Name.ToString()));
        emitter.Emit(new Scalar(SerializeSubjectSetRewrite(relation.SubjectSetRewrite)));
        emitter.Emit(new MappingEnd());
    }

    private static string SerializeSubjectSetRewrite(SubjectSetRewrite rewrite) =>
        rewrite switch
        {
            ThisRewrite => "this",
            ComputedSubjectSetRewrite computed => computed.Relation.ToString(),
            TupleToSubjectSetRewrite tuple => $"({tuple.TuplesetRelation}, {tuple.ComputedSubjectSetRelation})",
            UnionRewrite union => string.Join(" | ", union.Children.Select(SerializeSubjectSetRewrite)),
            IntersectionRewrite intersection => string.Join(" & ", intersection.Children.Select(SerializeSubjectSetRewrite)),
            ExclusionRewrite exclusion => $"{SerializeSubjectSetRewrite(exclusion.Include)} ! {SerializeSubjectSetRewrite(exclusion.Exclude)}",
            _ => throw new NotSupportedException($"Rewrite type {rewrite.GetType().Name} is not supported for serialization")
        };
}
