using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Infrastructure;
using SupportTicketTriage.Infrastructure.Events;

namespace SupportTicketTriage.Tests.Api;

public sealed class AzureConfigurationTests
{
    [Fact]
    public void AzureProvider_WithMissingNamespace_FailsWithoutSilentFallback()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(CreateConfig(
            provider: "AzureServiceBus",
            fullyQualifiedNamespace: "",
            queueName: "ticket-created"));

        using var provider = services.BuildServiceProvider();
        var error = Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IEventPublisher>());

        Assert.Contains("AzureServiceBus", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InMemoryEventPublisher", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AzureProvider_WithConnectionStringAndNoNamespace_ResolvesPublisher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(CreateConfig(
            provider: "AzureServiceBus",
            fullyQualifiedNamespace: "",
            queueName: "ticket-created",
            connectionString: "Endpoint=sb://demo.servicebus.windows.net/;SharedAccessKeyName=sender;SharedAccessKey=local-test-key"));

        await using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IEventPublisher>();

        Assert.IsNotType<InMemoryEventPublisher>(publisher);
    }

    [Fact]
    public void AzureProvider_WithInvalidConnectionString_FailsWithoutSilentFallback()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(CreateConfig(
            provider: "AzureServiceBus",
            fullyQualifiedNamespace: "",
            queueName: "ticket-created",
            connectionString: "not-a-connection-string"));

        using var provider = services.BuildServiceProvider();
        var error = Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IEventPublisher>());

        Assert.Contains("ConnectionString", error.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("InMemoryEventPublisher", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AzureProvider_WithConnectionStringForDifferentQueue_Fails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(CreateConfig(
            provider: "AzureServiceBus",
            fullyQualifiedNamespace: "",
            queueName: "ticket-created",
            connectionString: "Endpoint=sb://demo.servicebus.windows.net/;SharedAccessKeyName=sender;SharedAccessKey=local-test-key;EntityPath=other-queue"));

        using var provider = services.BuildServiceProvider();
        var error = Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IEventPublisher>());

        Assert.Contains("ConnectionString", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownProvider_FailsClearly()
    {
        var services = new ServiceCollection();
        var error = Assert.Throws<InvalidOperationException>(() =>
            services.AddInfrastructure(CreateConfig(provider: "RabbitMq")));

        Assert.Contains("RabbitMq", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no silent fallback", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IConfiguration CreateConfig(
        string provider,
        string fullyQualifiedNamespace = "demo.servicebus.windows.net",
        string queueName = "ticket-created",
        string connectionString = "") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Data Source=:memory:",
                ["EventPublishing:Provider"] = provider,
                ["AzureServiceBus:FullyQualifiedNamespace"] = fullyQualifiedNamespace,
                ["AzureServiceBus:QueueName"] = queueName,
                ["AzureServiceBus:ConnectionString"] = connectionString
            })
            .Build();
}
