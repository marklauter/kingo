using Kingo.Testing;
using System.Reflection;

namespace Kingo.Sdl.Tests.Architecture;

public sealed class ArchitectureTests()
    : AdapterArchitectureTestsBase(Assembly.Load("Kingo.Sdl"), @"^Kingo\.Sdl(\..*)?$");
