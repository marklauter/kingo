using Kingo.Testing;
using System.Reflection;

namespace Kingo.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo"), @"^Kingo(\..*)?$");
