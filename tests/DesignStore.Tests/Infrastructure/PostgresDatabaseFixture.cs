using DesignStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DesignStore.Tests.Infrastructure;

/// <summary>
/// Creates a random, isolated PostgreSQL database per test collection and drops it afterwards.
/// Default server: local PostgreSQL on localhost:5432 as user postgres; the password comes from the standard
/// pgpass file (%APPDATA%\postgresql\pgpass.conf), never from the repository.
/// Override with DESIGNSTORE_TEST_CONNECTION containing the {database} placeholder.
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"designstore_tests_{Guid.NewGuid():N}";
    public string ConnectionString => (Environment.GetEnvironmentVariable("DESIGNSTORE_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Username=postgres;Database={database};Timeout=30")
        .Replace("{database}", _databaseName, StringComparison.OrdinalIgnoreCase);

    public ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options);

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

[CollectionDefinition("PostgreSQL database")]
public sealed class PostgresDatabaseCollection : ICollectionFixture<PostgresDatabaseFixture>;
