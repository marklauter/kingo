using Kingo.Storage.Context;
using Kingo.Storage.Sqlite;

namespace Kingo.Storage.Tests.Sqlite;

public class SqliteTests
    : IAsyncLifetime
{
    public DbContext Context { get; }
    protected void AddMigration(string name, string script) =>
        migrations = migrations.Add(name, script);

    private Migrations migrations = Migrations.Empty();
    private readonly string dbName = $"{Guid.NewGuid()}.sqlite";

    public SqliteTests() =>
        Context = new(
            new SqliteConnectionFactory(
                new(
                    $"Data Source={dbName}",
                    true)));

    public async Task InitializeAsync() =>
        await Context.ApplyMigrationsAsync(migrations, CancellationToken.None);

    public Task DisposeAsync()
    {
        Context.ClearAllPools();
        if (File.Exists(dbName))
            File.Delete(dbName);

        return Task.CompletedTask;
    }
}

