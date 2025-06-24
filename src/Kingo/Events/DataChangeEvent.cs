using Kingo.Facts;

namespace Kingo.Events;

public abstract record DataChangeEvent(Subject Author, DateTime Timestamp);

