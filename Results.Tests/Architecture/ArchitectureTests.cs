using Kingo.Testing;
using System.Reflection;

namespace Results.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Results"), @"^Results(\..*)?$");
