using Kingo.Facts;
using System.Text.Json.Serialization;

namespace Kingo.Specifications;

[method: JsonConstructor]
public sealed record Ns(
    Identifier Name,
    IEnumerable<Relationship> Relationships);

[method: JsonConstructor]
public sealed record Relationship(
    Identifier Name,
    SubjectSetRewrite SubjectSetRewrite);

[method: JsonConstructor]
public sealed record SubjectSetRewrite()
{

}

// union, intersection, and exclusion

/*
// protobuf namespace configuration

name: "doc"

 relation { name: "owner" }

 relation {
 name: "editor"
 userset_rewrite {
 union {
 child { _this {} }
 child { computed_userset { relation: "owner" } }
 } } }

 relation {
 name: "viewer"
 userset_rewrite {
 union {
 child { _this {} }
 child { computed_userset { relation: "editor" } }
 child { tuple_to_userset {
 tupleset { relation: "parent" }
 computed_userset {
 object: $TUPLE_USERSET_OBJECT # parent folder
 relation: "viewer"
 } } }
 } } }
 Figure 1: Simple namespace configuration with concentric
 relations on documents. All owners are editors, and all ed
itors are viewers. Further, viewers of the parent folder are
 also viewers of the document
*/
