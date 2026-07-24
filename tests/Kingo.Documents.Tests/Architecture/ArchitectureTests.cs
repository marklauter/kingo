using Kingo.Testing;
using System.Reflection;

namespace Kingo.Documents.Tests.Architecture;

public sealed class ArchitectureTests()
    : AdapterArchitectureTestsBase(Assembly.Load("Kingo.Documents"), @"^Kingo\.Documents(\..*)?$");
