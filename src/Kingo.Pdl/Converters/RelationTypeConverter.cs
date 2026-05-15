using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Kingo.Pdl.Converters;

internal sealed class RelationTypeConverter
    : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Relation);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        // Simple case: "- owner"
        if (parser.TryConsume<Scalar>(out var scalar))
            return new Relation(RelationIdentifier.From(scalar.Value));

        // Complex case: "- editor: this | owner"
        if (parser.TryConsume<MappingStart>(out _))
        {
            var name = parser.Consume<Scalar>().Value;
            var expression = parser.Consume<Scalar>().Value;
            _ = parser.Consume<MappingEnd>();

            try
            {
                var rewrite = RewriteExpressionParser.Parse(expression);
                return new Relation(RelationIdentifier.From(name), rewrite);
            }
            catch (PdlParseException ex)
            {
                throw new YamlException($"Failed to parse rewrite expression '{expression}': {ex.Message}", ex);
            }
        }

        throw new YamlException("Unexpected token type for Relation deserialization. Expected a scalar or a mapping.");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not Relation relation)
            throw new ArgumentException($"Expected Relation but got {value?.GetType()?.Name ?? "null"}");

        // Simple relation with ThisRewrite: emit as scalar "- owner"
        if (relation.SubjectSetRewrite is ThisRewrite)
        {
            emitter.Emit(new Scalar(relation.Name.ToString()));
            return;
        }

        // Complex relation: emit as mapping "- editor: this | owner"
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
            ExclusionRewrite exclusion => SerializeExclusion(exclusion),
            _ => throw new NotSupportedException($"Rewrite type {rewrite.GetType().Name} is not supported for serialization")
        };

    private static string SerializeExclusion(ExclusionRewrite exclusion)
    {
        var includeStr = SerializeSubjectSetRewrite(exclusion.Include);
        var excludeStr = SerializeSubjectSetRewrite(exclusion.Exclude);

        // Exclusion (!) has higher precedence than union (|) and intersection (&).
        // Wrap the include side if it's union/intersection so the printed expression round-trips.
        if (exclusion.Include is UnionRewrite or IntersectionRewrite)
            includeStr = $"({includeStr})";

        return $"{includeStr} ! {excludeStr}";
    }
}
