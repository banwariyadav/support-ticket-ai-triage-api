# EV02 live Azure Service Bus smoke evidence

**Status: PASS**  
**Date:** 2026-11-05  
**Environment:** Windows development workspace `E:\Projects\20261105`

## What was verified

- `AzureServiceBusEventPublisher` is selected when `EventPublishing:Provider=AzureServiceBus`.
- The latest send authenticated with the namespace shared access connection string (`AzureServiceBus:ConnectionString` in user secrets). The API process did not have Azure CLI on its path and did not use `az login`.
- `POST /api/tickets` returned **HTTP 201** at 2026-11-04T19:51:30Z. The response `id` matches the published ticket.
- The API log confirmed the broker send:

```text
Published TicketCreated to Azure Service Bus. TicketId=cf3d01dc-652e-46d3-baf7-52743d82558a EventId=efe0faf5-fa68-4601-92e1-45b127bed462 CorrelationId=d06d088f-8835-4b0b-95d9-1a63e13def1e QueueName=ticket-created Namespace=sb-supportticket-triage-dev.servicebus.windows.net Authentication=ConnectionString
```

- An earlier send on the same queue used `DefaultAzureCredential` after `az login` and the **Azure Service Bus Data Sender** role. That message was HTTP 201 at 2026-11-04T19:33:14Z, ticket `31c65e37-1c10-4b00-a8ce-5087af5290c6`, event `075accd7-76dc-447f-8422-ec29baa2acf0`. The operator confirmed that message was visible in Azure Portal Service Bus Explorer. It was peeked, not received-and-deleted.

## Sanitized message metadata (connection-string send)

| Field | Value |
|---|---|
| Namespace | `sb-supportticket-triage-dev.servicebus.windows.net` |
| Queue | `ticket-created` |
| Resource group | `rg-supportticket-dev` |
| Authentication | Connection string (`RootManageSharedAccessKey`, Send). The key is not recorded here. |
| TicketId | `cf3d01dc-652e-46d3-baf7-52743d82558a` |
| MessageId / EventId | `efe0faf5-fa68-4601-92e1-45b127bed462` |
| CorrelationId | `d06d088f-8835-4b0b-95d9-1a63e13def1e` |
| Subject | `TicketCreated` |
| ContentType | `application/json` |
| schemaVersion | `1.0` |

The event body contains only event type, schema version, event id, ticket id, category, priority, correlation id, and occurred-at UTC. Customer name, email, and description are not on the message.

## Earlier attempts that did not publish

Two earlier `POST` calls saved a ticket and returned **503**. Neither message reached the queue: the first had no Azure credential, and the second lacked the Send claim. Those ticket ids are not evidence of a broker send.

## Automated tests

`dotnet test` on 2026-11-05: **32 passed, 0 failed**. The suite does not call Azure. Missing Azure configuration fails startup. There is no silent InMemory fallback. Connection-string configuration is covered with a fake connection string and does not open a live namespace.
