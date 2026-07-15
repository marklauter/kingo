using Kingo.Testing;
using System.Reflection;

namespace Kingo.Serialization.Pdl.Tests.Architecture;

public sealed class ArchitectureTests()
    : AdapterArchitectureTestsBase(Assembly.Load("Kingo.Serialization.Pdl"), @"^Kingo\.Serialization\.Pdl(\..*)?$");
