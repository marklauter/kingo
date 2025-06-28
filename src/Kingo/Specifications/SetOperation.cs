using System.Text.Json.Serialization;

namespace Kingo.Specifications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SetOperation
{
    Union,
    Intersection,
    Exclusion,
}
