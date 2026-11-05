using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportTicketTriage.Infrastructure.Persistence;

namespace SupportTicketTriage.Tests.Persistence;

public sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteTestDatabase(SqliteConnection connection, ApplicationDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public ApplicationDbContext Context { get; }

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new SqliteTestDatabase(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
