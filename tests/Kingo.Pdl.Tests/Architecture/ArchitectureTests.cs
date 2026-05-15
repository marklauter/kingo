using Kingo.Testing;
using System.Reflection;

namespace Kingo.Pdl.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo.Pdl"), @"^Kingo\.Pdl(\..*)?$");
