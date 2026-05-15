using Kingo.Testing;
using System.Reflection;

namespace Kingo.Serialization.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo.Serialization"), @"^Kingo\.Serialization(\..*)?$");
