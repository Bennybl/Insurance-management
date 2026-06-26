using InsuranceManagement.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsuranceManagement.Tests;

public class TestDatabase : IDisposable
{
    private readonly SqliteConnection connection;

    private TestDatabase(SqliteConnection connection, AppDbContext context)
    {
        this.connection = connection;
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new TestDatabase(connection, context);
    }

    public void Dispose()
    {
        Context.Dispose();
        connection.Dispose();
    }
}
