using Kingo.Testing;
using System.Reflection;

namespace Kingo.Serialization.Json.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo.Serialization.Json"), @"^Kingo\.Serialization\.Json(\..*)?$");
