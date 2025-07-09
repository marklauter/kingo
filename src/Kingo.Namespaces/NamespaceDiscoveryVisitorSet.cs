using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Namespaces;

public sealed class NamespaceDiscoveryVisitorSet(Arr<INamespaceDiscoveryVisitor> visitors)
    : INamespaceDiscoveryVisitor
{
    public INamespaceDiscoveryVisitor With(INamespaceDiscoveryVisitor visitor)
        => new NamespaceDiscoveryVisitorSet(visitors.Add(visitor));

    public Either<Error, Unit> OnNamespace(Namespace ns) =>
        visitors.Fold(
            Either<Error, Unit>.Right(Unit.Default),
            (e, visitor) =>
            e.Bind(_ => visitor.OnNamespace(ns)));

    public Either<Error, Unit> OnRelationship(Relationship relationship) =>
        visitors.Fold(
            Either<Error, Unit>.Right(Unit.Default),
            (e, visitor) =>
            e.Bind(_ => visitor.OnRelationship(relationship)));

    public Either<Error, Unit> OnResource(Identifier resource) =>
        visitors.Fold(
            Either<Error, Unit>.Right(Unit.Default),
                (e, visitor) =>
                e.Bind(_ => visitor.OnResource(resource)));

}

