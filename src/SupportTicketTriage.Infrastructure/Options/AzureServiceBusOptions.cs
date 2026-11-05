using System.ComponentModel.DataAnnotations;

namespace SupportTicketTriage.Infrastructure.Options;

public sealed class AzureServiceBusOptions
{
    public const string SectionName = "AzureServiceBus";

    public string FullyQualifiedNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Namespace or queue shared access connection string. When set, the client uses this
    /// secret and does not call <c>DefaultAzureCredential</c> or Azure CLI.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string QueueName { get; set; } = "ticket-created";

    public bool UsesConnectionString => !string.IsNullOrWhiteSpace(ConnectionString);
}
