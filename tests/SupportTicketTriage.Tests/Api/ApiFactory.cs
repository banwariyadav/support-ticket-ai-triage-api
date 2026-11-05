using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Infrastructure.Persistence;
using SupportTicketTriage.Tests.Fakes;

namespace SupportTicketTriage.Tests.Api;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"stt-{Guid.NewGuid():N}.db");

    public FakeEventPublisher Publisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("https_port", string.Empty);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("EventPublishing:Provider", "InMemory");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher>(Publisher);

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        TryDelete(_dbPath);
        TryDelete(_dbPath + "-shm");
        TryDelete(_dbPath + "-wal");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
    }
}
