using Kingo.Testing;
using System.Reflection;

namespace Kingo.Serialization.Sdl.Tests.Architecture;

public sealed class ArchitectureTests()
    : AdapterArchitectureTestsBase(Assembly.Load("Kingo.Serialization.Sdl"), @"^Kingo\.Serialization\.Sdl(\..*)?$");
