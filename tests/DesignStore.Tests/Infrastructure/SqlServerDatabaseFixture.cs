using DesignStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DesignStore.Tests.Infrastructure;

public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"DesignStoreTests_{Guid.NewGuid():N}";
    public string ConnectionString => Environment.GetEnvironmentVariable("DESIGNSTORE_TEST_CONNECTION")
        ?.Replace("{database}", _databaseName, StringComparison.OrdinalIgnoreCase)
        ?? $"Server=(localdb)\\WildSpiritLocalDb;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30";

    public ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateDb();
        await db.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition("SqlServer database")]
public sealed class SqlServerDatabaseCollection : ICollectionFixture<SqlServerDatabaseFixture>;
