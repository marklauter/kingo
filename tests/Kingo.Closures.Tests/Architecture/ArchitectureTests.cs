using Kingo.Testing;
using System.Reflection;

namespace Kingo.Closures.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo.Closures"), @"^Kingo\.Closures(\..*)?$");
