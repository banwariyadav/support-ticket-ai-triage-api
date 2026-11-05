# Azure Service Bus prerequisites

This project **requires** a real Azure Service Bus queue named `ticket-created` for live/demo acceptance. In-memory publishing is for local development and automated tests only. The API never provisions Azure resources at startup and never falls back from `AzureServiceBus` to `InMemory`.

## Required resources

1. An Azure subscription with permission to create (or use) a Service Bus namespace.
2. A Service Bus namespace. Standard tier is recommended if you enable duplicate detection; Basic does not support it.
3. A **queue** named `ticket-created`.
4. Networking that allows the developer machine or host to reach the namespace.

Record only non-secret values:

- Fully qualified namespace: `your-namespace.servicebus.windows.net`
- Queue name: `ticket-created`

Do not commit connection strings, shared access keys, or subscription IDs unless they are already public in your organization and required.

## Identity and RBAC

The API authenticates in one of two ways:

- **Connection string.** Set `AzureServiceBus:ConnectionString` (environment variable `AzureServiceBus__ConnectionString`, or user secrets). A namespace or queue shared access policy with **Send** is enough. This path does not use `az login` or the Data Sender role. Do not commit the value.
- **`DefaultAzureCredential`.** Used only when the connection string is empty. Locally this typically means `az login`. Assign **Azure Service Bus Data Sender** to that identity, scoped to the namespace or the `ticket-created` queue.

To peek messages in Portal Service Bus Explorer (do not receive-and-delete), the observer needs **Azure Service Bus Data Receiver** or an equivalent Explorer role. Send-only credentials cannot peek.

## Configuration

```json
{
  "EventPublishing": { "Provider": "AzureServiceBus" },
  "AzureServiceBus": {
    "FullyQualifiedNamespace": "your-namespace.servicebus.windows.net",
    "QueueName": "ticket-created"
  }
}
```

Environment variable equivalents:

```text
EventPublishing__Provider=AzureServiceBus
AzureServiceBus__FullyQualifiedNamespace=your-namespace.servicebus.windows.net
AzureServiceBus__QueueName=ticket-created
```

If `Provider` is `AzureServiceBus` and the namespace or queue is missing, the host **fails at startup**. It does not switch to the in-memory publisher.

## Duplicate detection

Duplicate detection is optional. If enabled at queue creation, record the detection window. It only deduplicates broker messages that share `MessageId` inside that window. It does **not** prevent duplicate tickets when a client retries POST.

## Provisioning (manual)

Portal: create a Service Bus namespace → Queues → add `ticket-created` → Access control → assign Data Sender.

Azure CLI (after `az login`):

```bash
az servicebus namespace create --resource-group <rg> --name <namespace> --sku Standard --location <region>
az servicebus queue create --resource-group <rg> --namespace-name <namespace> --name ticket-created
az role assignment create --assignee <your-user-or-principal> --role "Azure Service Bus Data Sender" --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.ServiceBus/namespaces/<namespace>
```

Provisioning is outside application startup.
