using Kingo.Facts;

namespace Kingo.Events;

public sealed record DisassociationEvent(Subject Author, DateTime Timestamp, Association Association)
    : DataChangeEvent(Author, Timestamp);

