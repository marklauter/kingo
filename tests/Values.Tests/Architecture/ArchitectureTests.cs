using Kingo.Testing;
using System.Reflection;

namespace Values.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Values"), @"^Values(\..*)?$");
