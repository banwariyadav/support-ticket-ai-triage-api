# Live Azure Service Bus smoke test. NOT part of `dotnet test`.
# Prerequisites: az CLI, az login, Data Sender (and Receiver to peek), provisioned queue.

param(
    [Parameter(Mandatory = $true)]
    [string] $FullyQualifiedNamespace,

    [string] $QueueName = "ticket-created",
    [string] $ApiBaseUrl = "http://localhost:5079"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "BLOCKED: Azure CLI (az) is not installed. Live Service Bus verification cannot run."
}

Write-Host "Using EventPublishing=AzureServiceBus queue=$QueueName namespace=$FullyQualifiedNamespace"
Write-Host "POST $ApiBaseUrl/api/tickets"

$correlationId = [guid]::NewGuid().ToString()
$body = @{
    customerName = "Smoke Tester"
    email        = "smoke.tester@example.com"
    description  = "Smoke test: production application down. Non-sensitive verification only."
} | ConvertTo-Json

$response = Invoke-WebRequest `
    -Uri "$ApiBaseUrl/api/tickets" `
    -Method POST `
    -ContentType "application/json" `
    -Headers @{ "X-Correlation-ID" = $correlationId } `
    -Body $body

if ($response.StatusCode -ne 201) {
    Write-Error "FAIL: expected HTTP 201, received $($response.StatusCode)"
}

$payload = $response.Content | ConvertFrom-Json
Write-Host "HTTP 201 TicketId=$($payload.id) CorrelationId=$correlationId"
Write-Host "Peek queue '$QueueName' in Azure Portal Service Bus Explorer. Do not receive-and-delete."
Write-Host "Confirm Subject=TicketCreated, schemaVersion=1.0, matching ticketId, no customer PII."
