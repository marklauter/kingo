using Kingo.Storage.SQLite;

namespace Kingo.Storage.Tests;

public sealed class SQLiteStoreTests
{
    [Fact]
    public void Test()
    {
        var store = new SQLiteStore();
        store.Foo();
    }
}
