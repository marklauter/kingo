using Kingo.Clock;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Facts;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "<Pending>")]
public sealed record Namespace(string Id, LogicalTime Version) : Fact(Id, Version);

