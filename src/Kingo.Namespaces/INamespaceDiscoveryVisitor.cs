using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Namespaces;

public interface INamespaceDiscoveryVisitor
{
    Either<Error, Unit> OnNamespace(Namespace ns);
    Either<Error, Unit> OnResource(Identifier resource);
    Either<Error, Unit> OnRelationship(Relationship relationship);
}

