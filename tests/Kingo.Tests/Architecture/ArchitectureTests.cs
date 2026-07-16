using Kingo.Testing;
using System.Reflection;

namespace Kingo.Tests.Architecture;

/// <summary>
/// <c>Kingo</c> is the shared kernel — the identifiers the statement model, the config model, and the services all
/// speak. The flat namespace is the invariant: a sub-namespace here means a model has been parked in the kernel
/// instead of given its own project.
/// </summary>
public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo"), @"^Kingo$");
