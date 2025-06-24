using Kingo.Clock;
using Kingo.Facts;

namespace Kingo.Storage;

public sealed class FactStore
{
    public F WriteAsync<F>() where F : Fact => throw new NotImplementedException();
    public F ReadAsync<F>(string id, LogicalTime version) where F : Fact => throw new NotImplementedException();
}
