using Kingo.Testing;
using System.Reflection;

namespace Kingo.Serialization.Json.Tests.Architecture;

public sealed class ArchitectureTests()
    : AdapterArchitectureTestsBase(Assembly.Load("Kingo.Serialization.Json"), @"^Kingo\.Serialization\.Json(\..*)?$");
