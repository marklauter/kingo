using Kingo.Testing;
using System.Reflection;

namespace Kingo.Serialization.Yaml.Tests.Architecture;

public sealed class ArchitectureTests()
    : AdapterArchitectureTestsBase(Assembly.Load("Kingo.Serialization.Yaml"), @"^Kingo\.Serialization\.Yaml(\..*)?$");
