using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Application.Tickets;
using SupportTicketTriage.Infrastructure.Ai;
using SupportTicketTriage.Infrastructure.Events;
using SupportTicketTriage.Infrastructure.Options;
using SupportTicketTriage.Infrastructure.Persistence;
using SupportTicketTriage.Infrastructure.Time;

namespace SupportTicketTriage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITicketService, TicketService>();
        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IUtcClock, SystemUtcClock>();
        services.AddSingleton<IAiAnalysisService, MockAiAnalysisService>();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is required.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        AddEventPublishing(services, configuration);
        return services;
    }

    private static void AddEventPublishing(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration[$"{EventPublishingOptions.SectionName}:Provider"]
            ?? EventPublishingOptions.AzureServiceBus;

        if (string.Equals(provider, EventPublishingOptions.InMemory, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
            return;
        }

        if (!string.Equals(provider, EventPublishingOptions.AzureServiceBus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unknown EventPublishing:Provider '{provider}'. Use '{EventPublishingOptions.InMemory}' or '{EventPublishingOptions.AzureServiceBus}'. There is no silent fallback.");
        }

        services.AddOptions<AzureServiceBusOptions>()
            .Bind(configuration.GetSection(AzureServiceBusOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.UsesConnectionString || IsServiceBusNamespace(options.FullyQualifiedNamespace),
                "Set AzureServiceBus:ConnectionString, or set AzureServiceBus:FullyQualifiedNamespace to a host such as your-namespace.servicebus.windows.net.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.QueueName),
                "AzureServiceBus:QueueName is required when EventPublishing:Provider is AzureServiceBus.")
            .Validate(
                options => !options.UsesConnectionString || IsUsableConnectionString(options),
                "AzureServiceBus:ConnectionString must be a Service Bus shared access connection string whose EntityPath is empty or matches AzureServiceBus:QueueName.")
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            var clientOptions = new ServiceBusClientOptions
            {
                RetryOptions = new ServiceBusRetryOptions
                {
                    Mode = ServiceBusRetryMode.Exponential,
                    MaxRetries = 3,
                    Delay = TimeSpan.FromMilliseconds(800),
                    MaxDelay = TimeSpan.FromSeconds(10),
                    TryTimeout = TimeSpan.FromSeconds(15)
                }
            };

            if (options.UsesConnectionString)
            {
                return new ServiceBusClient(options.ConnectionString, clientOptions);
            }

            return new ServiceBusClient(options.FullyQualifiedNamespace, new DefaultAzureCredential(), clientOptions);
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            return sp.GetRequiredService<ServiceBusClient>().CreateSender(options.QueueName);
        });

        services.AddSingleton<IServiceBusMessageSender, ServiceBusSenderAdapter>();
        services.AddSingleton<IEventPublisher, AzureServiceBusEventPublisher>();
    }

    private static bool IsServiceBusNamespace(string? fullyQualifiedNamespace) =>
        !string.IsNullOrWhiteSpace(fullyQualifiedNamespace)
        && fullyQualifiedNamespace.Contains(".servicebus.windows.net", StringComparison.OrdinalIgnoreCase);

    private static bool IsUsableConnectionString(AzureServiceBusOptions options)
    {
        try
        {
            var parsed = ServiceBusConnectionStringProperties.Parse(options.ConnectionString);
            var hasSecret = !string.IsNullOrWhiteSpace(parsed.SharedAccessKey)
                || !string.IsNullOrWhiteSpace(parsed.SharedAccessSignature);
            if (!hasSecret || !IsServiceBusNamespace(parsed.FullyQualifiedNamespace))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(parsed.EntityPath)
                || string.Equals(parsed.EntityPath, options.QueueName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return false;
        }
    }
}
