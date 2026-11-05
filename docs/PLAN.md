# Support Ticket Intake & AI Triage API — Implementation Plan

**Version:** 2.0  
**Status:** Ready for staged implementation, pending Azure access prerequisites  
**Date:** 2026-11-04  
**Specification:** [SPEC.md](SPEC.md)  
**Execution tool:** Cursor Agent with developer validation after each phase

## 1. Delivery objective and five locked decisions

Implement a working .NET 10 API with SQLite, deterministic mock AI, and **real Azure Service Bus Queue publishing**. Preserve the client's three API endpoints, xUnit coverage, Swagger and documentation while adding live Azure evidence.

**Non-negotiable decisions:**

1. **Azure Service Bus queue is mandatory**; actual cloud send and verification are a final acceptance gate.
2. **SQLite remains** the EF Core database; use UTC `DateTime`, not SQLite-ordered `DateTimeOffset`, and validate SQL-side sorting.
3. **Mock AI is default and required**; no live Azure OpenAI integration needed.
4. **Direct publish after SQLite commit**; acknowledge partial failures and duplicates, no outbox in v2.0.
5. **Automated tests never require Azure**; a distinct live Service Bus smoke test is mandatory.

The client PDF labels Service Bus optional. This plan deliberately promotes it to a **project-mandated extension**, not a statement about the original brief.

## 2. Prerequisites and blockers — check before coding

- [ ] .NET 10 SDK installed; Cursor and Git available.
- [ ] Azure subscription and permission to use/create a Service Bus namespace and `ticket-created` queue (or pre-existing resources).
- [ ] Azure Service Bus namespace FQDN, queue name, selected tier/region, networking and required firewall access.
- [ ] Sender identity able to use **Azure Service Bus Data Sender** via Azure RBAC; local sign-in via `az login` or an approved managed identity.
- [ ] If verifying through portal/peek, authorized observer with **Azure Service Bus Data Receiver** permission (send-only credentials cannot peek).
- [ ] Cloud resource cost/permission is accepted; do not place credentials in repo.

**Hard blocker rule:** If Azure access or queue provisioning is unavailable, keep building the locally testable core but mark **mandatory cloud acceptance blocked**, not complete. Do not substitute in-memory evidence.

## 3. Implementation schedule (target: 7h 35m focused work)

The original client gives about **6–8 focused hours**; mandatory Azure infrastructure access and debug time may exceed this. The estimates below assume access is ready. Prioritize the whole required workflow rather than bonus features.

| Phase | Work | Target | Exit condition |
|---|---|---:|---|
| 0 | Verify requirements, .NET solution and project references | 30 min | Solution builds |
| 1 | Azure Service Bus prerequisites, queue and identity | 45 min | Queue exists and sender auth is feasible |
| 2 | Entities, UTC timestamps, EF Core SQLite + migration | 45 min | Migration applies and SQLite sorting works |
| 3 | `IAiAnalysisService`, deterministic mock and tests | 40 min | AI tests pass offline |
| 4 | TicketService and 3 HTTP endpoints | 75 min | Create/get/list work locally |
| 5 | Event contract + Azure Service Bus + in-memory adapter | 60 min | Valid message sent to real queue |
| 6 | Validation, ProblemDetails, structured logging | 40 min | Error scenarios and safe logs checked |
| 7 | Unit/integration tests, failure-path checks | 75 min | `dotnet test` green without Azure |
| 8 | Swagger, live smoke-test evidence, README, Git | 45 min | Cloud verification and submission evidence complete |
| **Total** | | **455 min (7h 35m)** | All mandatory gates satisfied |

If time is constrained, reduce optional polish, not the core endpoints, automated tests, or **real Service Bus verification**. Escalate unavailable Azure credentials/permissions immediately.

## 4. Stage-by-stage tasks and checkpoints

### Phase 0 — Architecture and workspace

- [ ] Place `SPEC.md` and `PLAN.md` under `docs/` in Cursor workspace.
- [ ] Create `Api`, `Application`, `Domain`, `Infrastructure`, and `Tests` projects and references from SPEC §2.1.
- [ ] Add compatible .NET 10 packages for EF Core SQLite, `Azure.Messaging.ServiceBus`, `Azure.Identity`, Swagger UI, xUnit and `Microsoft.AspNetCore.Mvc.Testing`.
- [ ] Configure DI, appsettings sample values, `.gitignore` (DB files, binaries, secrets), nullable types.
- [ ] Decide narrow persistence interface; no CQRS/MediatR/generic repository.
- [ ] **Gate:** run `dotnet restore && dotnet build`; record actual results.

**Cursor prompt:** `Read docs/SPEC.md and docs/PLAN.md. Build only the .NET 10 solution structure and DI composition root defined in Phase 0. Do not create unrelated infrastructure. Run dotnet build and report failures truthfully.`

### Phase 1 — Azure first: resource and permissions

- [ ] Select or create Service Bus namespace; create queue `ticket-created` (recommended Standard if using duplicate detection).
- [ ] Record FQDN and queue name as non-secret configuration; do not commit subscription IDs if unnecessary.
- [ ] Confirm the running identity's **Data Sender** role and network connectivity; determine required Receiver/Explorer permission for verification.
- [ ] Document manual Azure Portal or CLI setup in README; provision outside API startup.
- [ ] Decide whether duplicate detection is enabled at queue creation, and record window/tier if used.
- [ ] **Gate:** signed-in developer can authenticate and the queue is accessible; no Service Bus credentials in source.

**Cursor prompt:** `Implement configuration and documented prerequisites for the mandatory ticket-created Azure Service Bus queue. Use DefaultAzureCredential with a sender RBAC role, and validate required config. Do not fake an Azure connection or hardcode credentials.`

### Phase 2 — Database model and migration

- [ ] Add `Ticket` and `TicketAnalysis`, TicketId PK/FK one-to-one relationship and constraints.
- [ ] Store `CreatedDate`/`AnalysedDate` as **UTC DateTime** in EF Core SQLite; enforce UTC conversion on read/serialization.
- [ ] Add index on created date; sensible indexes on analysis priority where useful.
- [ ] Generate/apply migration; add isolated database integration fixture.
- [ ] Add a test proving correct date sort in SQLite, not client-side.
- [ ] **Gate:** migration and provider-side ordering test pass.

**Cursor prompt:** `Implement SPEC §4 exactly: SQLite via EF Core 10, UTC DateTime timestamps and Ticket/TicketAnalysis one-to-one mapping. Create migration, verify the provider can sort CreatedDate in SQL, and avoid DateTimeOffset ordering issues.`

### Phase 3 — Mock AI contract

- [ ] Define `IAiAnalysisService`, `AiAnalysisResult` and deterministic `MockAiAnalysisService`.
- [ ] Define category/priority vocabularies and precedence; separate category from urgency.
- [ ] Normalize/truncate descriptions and handle unknown text without invented assertions.
- [ ] Add tests for Billing, Technical, Account, General; Low/Medium/High/Critical; whitespace, mixed keywords.
- [ ] **Gate:** AI tests pass without network and SDK secrets.

**Cursor prompt:** `Implement SPEC §5 using only deterministic MockAiAnalysisService. Make classifications testable and repeatable. No Azure OpenAI integration. Run focused xUnit tests.`

### Phase 4 — API and orchestration

- [ ] Request and response contracts with no EF entities exposed.
- [ ] Implement POST, GET by ID, GET list with filters/pagination and stable SQL sort.
- [ ] TicketService orchestration: validate → classify → save both rows in one `SaveChangesAsync` → publish event → `201`.
- [ ] Inject `IEventPublisher` and keep controllers thin; use async/cancellation.
- [ ] Add status codes and route/Location semantics.
- [ ] **Gate:** all endpoints run locally with a test/in-memory publisher; end-to-end cloud proof follows Phase 5.

**Cursor prompt:** `Implement SPEC §3: 3 endpoints, DTOs, TicketService, SQLite-side pagination and UTC ordering. Save both entities first, then call IEventPublisher. Keep controllers thin and include CreatedAtAction Location. Do not silently return 201 when publication fails.`

### Phase 5 — **Mandatory Azure Service Bus integration**

- [ ] Create versioned `TicketCreatedEvent` with EventId, TicketId, Category, Priority, CorrelationId, OccurredAtUtc.
- [ ] Implement AzureServiceBusEventPublisher using long-lived ServiceBusClient/ServiceBusSender, Azure.Identity, and bounded retry behavior.
- [ ] Use `ticket-created` queue, JSON body, stable `MessageId`, `Subject=TicketCreated`, `ContentType=application/json`, `CorrelationId`, schemaVersion app property.
- [ ] Retain InMemoryEventPublisher for isolated tests/local runs, selected explicitly through configuration.
- [ ] Fail clearly for invalid/missing Azure configuration; no automatic fallback when provider `AzureServiceBus` is selected.
- [ ] Ensure send occurs strictly after committed SQLite write; document non-atomicity and duplicates.
- [ ] Start app with Azure provider, send a unique POST, and **peek/verify a real queue message** (record sanitized evidence).
- [ ] **Gate:** real Service Bus acceptance verified; in-memory test alone is **not sufficient**.

**Cursor prompt:** `Implement SPEC §6: real Azure Service Bus queue sender as mandatory, plus in-memory test adapter. Use DefaultAzureCredential and DI-managed ServiceBusClient/Sender. Preserve event schema and metadata exactly, do not log PII, send after DB commit, and demonstrate a real broker message. If Azure cannot be accessed, report BLOCKED, not PASS.`

### Phase 6 — Errors and logging

- [ ] Implement validation lengths, email syntax, pagination enum validation.
- [ ] Configure centralized `IExceptionHandler`/ProblemDetails for 400/404/500/503.
- [ ] Distinguish broker publish error from DB errors; broker error after commit yields 503 while retaining ticket.
- [ ] Log safe `TicketId`/`EventId`/`CorrelationId` and latency, not name/email/description or secrets.
- [ ] **Gate:** negative HTTP paths match SPEC and logs are inspected for PII.

**Cursor prompt:** `Implement SPEC §7: consistent ProblemDetails, broker 503 after DB commit, safe structured logging and no leaked PII. Preserve failed-send ambiguity and do not claim rolled-back data.`

### Phase 7 — Automated verification without Azure

- [ ] Unit tests for mock AI, event serialization and Azure sender message mapping using an injected adapter/fake.
- [ ] Order and failure tests: DB failure → no publish; send failure → DB persists, no 201.
- [ ] HTTP integration tests via WebApplicationFactory with temporary SQLite and fake publisher.
- [ ] Verify 201/Location, 400, 404, GET list, priority filter, stable date ordering.
- [ ] Ensure test suite never authenticates to Azure or requires cloud variables.
- [ ] **Gate:** `dotnet build && dotnet test` pass completely offline.

**Cursor prompt:** `Implement SPEC §8.1 unit and integration tests. Replace Azure dependencies in tests. Use isolated SQLite, verify actual persistence plus event order/metadata and 503 partial failure. Execute tests, show actual pass/fail results, and never assume cloud availability.`

### Phase 8 — Live smoke test, documentation and delivery

- [ ] Re-run Azure Service Bus smoke test with provider `AzureServiceBus`, send and **peek** event using authorized account; keep evidence sanitized.
- [ ] Verify `EventId`/`MessageId`, subject, JSON schema version, correlation ID, ticket ID and absence of PII.
- [ ] Run Swagger manually for 3 endpoints and capture successful scenarios.
- [ ] README: solution summary, diagram, exact .NET/SQLite setup, Azure prerequisites/RBAC, provider switches, test run, smoke test, non-atomicity and POST retries, assumptions, known limits.
- [ ] README **AI Usage Notes**: actual Cursor prompts, code reviews and 2–3 genuinely corrected/rejected AI outputs (do not invent).
- [ ] Check secrets, .gitignore, migration inclusion, source build from clean checkout; prepare Git link/ZIP.
- [ ] **Gate:** acceptance checklist signed off and evidence collected; if real cloud verification missing mark incomplete.

**Cursor prompt:** `Compare solution to SPEC §8.3/§9. Run actual build/tests; record verified and unverified items. Create a truthful README with 2–3 genuine AI-review examples. Perform real Azure Service Bus smoke verification and record sanitized evidence, or mark acceptance BLOCKED.`

## 5. Smoke test runbook (manual/live; NOT in dotnet test)

**Before demo:** ensure queue exists and the API identity has `Azure Service Bus Data Sender`. An operator who peeks needs a Data Receiver/appropriate role. Make sure cloud access is permitted by firewall/network rules.

1. Set environment variables (fill in your actual namespace):

   ```bash
   EventPublishing__Provider=AzureServiceBus
   AzureServiceBus__FullyQualifiedNamespace=YOUR_NAMESPACE.servicebus.windows.net
   AzureServiceBus__QueueName=ticket-created
   ```

   Set them through your OS/shell or launch profile; syntax above illustrates names/values, not shell-export syntax.

2. `az login` if testing locally with `DefaultAzureCredential` and required identity roles.
3. Run migrations (`dotnet ef database update ...` with correct startup and Infrastructure project args) and start the API.
4. POST a ticket with a clearly identifiable *non-sensitive test description*; require HTTP `201` and record `TicketId` / correlation info.
5. Use Azure Portal → Service Bus namespace → Queues → `ticket-created` → Service Bus Explorer → **Peek**. Do not receive-and-delete.
6. Inspect message properties and payload. Confirm `TicketId`, `MessageId`, `Subject`, `ContentType`, `CorrelationId` and `schemaVersion` match SPEC §6.2.
7. Save redacted screenshot or human-readable results under optional `evidence/` (do not commit PII, credentials or message content that includes confidential data).
8. If any step fails, record `BLOCKED/FAIL` with root cause and do not claim Service Bus accepted a message.

## 6. Risk register and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| No Azure access, insufficient RBAC, firewall | Mandatory cloud gate blocked | Validate in Phase 1, request sender/observer permissions early |
| Broker unavailable after DB commit | Persisted ticket, error response, potential duplicate on POST retry | Explicit 503, safe logs, document non-atomicity; do not auto-retry POST |
| Broker send timeout with unknown acceptance | Same event possibly already exists in broker | Stable event/message ID for same logical send, optional duplicate detection, explicit uncertain outcome |
| SQLite DateTime kind loss / sort behavior | Incorrect ordering or serialization | UTC mapping/DTO normalization and relational ordering integration test |
| SDK package/config mismatch | Failed build/startup | Pin compatible package versions, build early |
| Duplicate detection unavailable on chosen tier | Broker duplicate risk | Do not promise exactly-once; Standard tier if feature used |
| Azure-dependent automated tests | Unreproducible assessment | SDK mock seam + `WebApplicationFactory` fake publisher, no cloud test in default suite |
| Time budget exceeded | Incomplete deliverable | Focus scope; avoid Azure Functions/outbox/live OpenAI and optional polish |
| PII leakage in logs/events/evidence | Privacy risk | Strict event envelope, safe logging and evidence review |

## 7. Acceptance / traceability checklist

| ID | Deliverable | Evidence | Gate |
|---|---|---|---|
| F01 | POST persists ticket/analysis and returns 201 after publish | HTTP integration test | Mandatory |
| F02 | GET by ID (200/404) | Integration test | Mandatory |
| F03 | GET list, date sort, priority filter, pagination | SQLite integration test | Mandatory |
| AI01 | Deterministic offline mock | Unit tests | Mandatory |
| DATA01 | EF Core migration, UTC DateTime | Migration + sorting test | Mandatory |
| EV01 | Event abstraction + after-commit order | Unit/orchestration tests | Mandatory |
| EV02 | **Actual Service Bus Queue send and verification** | **Live smoke evidence** | **Mandatory** |
| XC01 | Validation, ProblemDetails, safe structured logs | Tests + log review | Mandatory |
| TEST01 | Entire automated suite works without Azure | `dotnet test` | Mandatory |
| DOC01 | README, setup, risks and honest AI Usage Notes | Review | Mandatory |

**Definition of done:** Every mandatory item above is verified, including EV02. "Tests passed offline" without live Service Bus verification is **not** full acceptance.

## 8. Cursor workflow and review discipline

- Use `SPEC.md` as technical source of truth and this file as execution order.
- One bounded Cursor prompt per phase, then inspect generated code and diffs.
- Run build/tests after each nontrivial phase. Treat Cursor claims as unverified until commands run.
- Record 2–3 *real* examples of AI-generated output reviewed/modified/rejected for final README.
- Keep decisions consistent across docs, code, migrations and tests.
- No additional Cursor skills are required for acceptance; optional rules/skills must defer to `SPEC.md` and `PLAN.md`.

## 9. Change log from v1.0

- Service Bus moved from optional Phase 8 to **mandatory Phases 1, 5 and 8**, with live smoke verification.
- Project remains SQLite/EF Core, now explicitly storing timestamps in UTC `DateTime` for provider-side sort.
- Deterministic mock AI remains the default; no live Azure OpenAI.
- Direct publish after DB commit formalized with `503`, acknowledged partial failure and duplicate risks; no outbox.
- Offline automated tests and cloud-only smoke test separated into distinct acceptance gates.
- Added cloud prerequisites, RBAC, queue metadata, evidence, risk register and traceability.
