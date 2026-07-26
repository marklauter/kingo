using Kingo.Testing;
using System.Reflection;

namespace Kingo.Tests.Architecture;

/// <summary>
/// <c>Kingo</c> is the shared kernel — the identifiers the fact side, the theory side, and the services all
/// speak. The flat namespace is the invariant: a sub-namespace here means a model has been parked in the kernel
/// instead of given its own project. <c>Kingo.Diagnostics</c> is the one sanctioned exception — the cross-cutting
/// error-code vocabulary is not a model, so it earns its own namespace rather than crowding the kernel root.
/// </summary>
public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo"), @"^Kingo(\.Diagnostics)?$");
