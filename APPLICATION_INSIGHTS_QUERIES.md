# Application Insights Queries

Useful KQL queries for the SSSKL backend in Application Insights. Run them in the Azure Portal: **Application Insights → Logs**. Change `ago(...)` to the period you need.

## Where the data comes from

The backend sends telemetry through OpenTelemetry and the Azure Monitor exporter (`UseAzureMonitor()` in `SSSKL.ServiceDefaults/Extensions.cs`). This only happens when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set.

| Table | Contents |
|---|---|
| `requests` | Incoming HTTP requests. Health checks are excluded and 401 responses are dropped, see `StatusCodeFilterProcessor`. |
| `dependencies` | SQL, blob storage and outgoing HTTP calls (web push, Turnstile), plus the internal spans `OrderCsvExportJob` and `StatsRecalculationJob`. |
| `exceptions` | Exceptions recorded on requests and logs. |
| `traces` | `ILogger` logs. Structured log parameters (e.g. `{JobId}`) are in `customDimensions`. |
| `customMetrics` | Runtime metrics and the business metrics from `SSSKL.Contracts/Telemetry/SssklTelemetry.cs`. |

Business metrics (meter `SSSKL`):

| Metric | Type | Dimensions | Meaning |
|---|---|---|---|
| `sssklv2.orders.created` | counter | `product` | Order lines saved |
| `sssklv2.orders.revenue` | counter (EUR) | `product` | Amount charged for orders |
| `sssklv2.topups.created` | counter | | Top-ups saved |
| `sssklv2.topups.amount` | counter (EUR) | | Amount added by top-ups |
| `sssklv2.push.sent` | counter | `outcome` (`success`, `expired`, `failed`) | Web push deliveries |
| `sssklv2.side_effects.failed` | counter | `source` | Failures after a change was committed (domain event handlers, notifications, live updates) |
| `sssklv2.jobs.duration` | histogram (s) | `job`, `outcome` | Background job durations |

The exporter sends counters as the increase per export interval, so add the values up with `sum(value)`. For a histogram, `valueSum / valueCount` is the average.

> If your Application Insights resource is workspace-based and you query the Log Analytics workspace directly, the tables are named `AppRequests`, `AppDependencies`, `AppExceptions`, `AppTraces` and `AppMetrics`, with PascalCase columns. The queries below use the classic names, which work in the Application Insights **Logs** blade.

---

## API health

### Request volume and failure rate per endpoint

```kusto
requests
| where timestamp > ago(24h)
| summarize
    Requests = count(),
    Failed = countif(success == false),
    P95ms = percentile(duration, 95)
    by name
| extend FailureRate = round(100.0 * Failed / Requests, 2)
| order by Requests desc
```

### Slowest endpoints

```kusto
requests
| where timestamp > ago(24h)
| summarize
    Count = count(),
    P50ms = percentile(duration, 50),
    P95ms = percentile(duration, 95),
    P99ms = percentile(duration, 99)
    by name
| where Count >= 10
| order by P95ms desc
| take 20
```

### Failed requests over time by status code

```kusto
requests
| where timestamp > ago(7d)
| where toint(resultCode) >= 400
| summarize count() by resultCode, bin(timestamp, 1h)
| render timechart
```

### Server errors (5xx) with their exception

```kusto
requests
| where timestamp > ago(24h)
| where toint(resultCode) >= 500
| project timestamp, name, resultCode, duration, operation_Id
| join kind=leftouter (
    exceptions
    | where timestamp > ago(24h)
    | project operation_Id, type, outerMessage, method
  ) on operation_Id
| project timestamp, name, resultCode, type, outerMessage, method, operation_Id
| order by timestamp desc
```

### Exceptions grouped by type

```kusto
exceptions
| where timestamp > ago(7d)
| summarize Count = count(), LastSeen = max(timestamp), Sample = any(outerMessage) by type, method
| order by Count desc
```

### Client errors from the global exception handler (404, 403, 409, 400)

`GlobalExceptionHandler` logs every handled 4xx with the exception type that the handler threw.

```kusto
traces
| where timestamp > ago(24h)
| where message startswith "Request " and message has "failed with"
| extend
    Method = tostring(customDimensions.Method),
    Path = tostring(customDimensions.Path),
    StatusCode = toint(customDimensions.StatusCode),
    ExceptionType = tostring(customDimensions.ExceptionType)
| summarize Count = count() by StatusCode, ExceptionType, Path
| order by Count desc
```

### Full trace of one request

Copy the `operation_Id` from any row above.

```kusto
let op = "<operation_Id>";
union requests, dependencies, traces, exceptions
| where operation_Id == op
| project timestamp, itemType, name, message, type, resultCode, duration, success, severityLevel
| order by timestamp asc
```

---

## CQRS / MediatR

### Commands and queries handled

`LoggingBehavior` logs `Handling {Request}` for every command and query.

```kusto
traces
| where timestamp > ago(24h)
| where message startswith "Handling "
| extend Request = tostring(customDimensions.Request)
| summarize Count = count() by Request
| order by Count desc
```

### Failing domain event handlers

`ResilientNotificationPublisher` logs and swallows failing domain event handlers, so these failures never show up as failed requests.

```kusto
traces
| where timestamp > ago(7d)
| where message startswith "Domain event handler"
| extend Handler = tostring(customDimensions.Handler), Event = tostring(customDimensions.Event)
| summarize Count = count(), LastSeen = max(timestamp) by Handler, Event
| order by Count desc
```

### MediatR license warnings

See `MEDIATR_LICENSE_SETUP.md`. No rows after the latest restart means that the license key was accepted.

```kusto
traces
| where timestamp > ago(1d)
| where message has "MediatR" and message has "licens"
| project timestamp, severityLevel, message
| order by timestamp desc
```

---

## Business metrics

### Orders per product per day

```kusto
customMetrics
| where timestamp > ago(30d)
| where name == "sssklv2.orders.created"
| extend Product = tostring(customDimensions["product"])
| summarize Orders = sum(value) by Product, Day = bin(timestamp, 1d)
| order by Day desc, Orders desc
```

### Revenue per day

```kusto
customMetrics
| where timestamp > ago(30d)
| where name == "sssklv2.orders.revenue"
| summarize RevenueEUR = round(sum(value), 2) by Day = bin(timestamp, 1d)
| render columnchart
```

### Top products by revenue

```kusto
customMetrics
| where timestamp > ago(30d)
| where name in ("sssklv2.orders.created", "sssklv2.orders.revenue")
| extend Product = tostring(customDimensions["product"])
| summarize
    Orders = sumif(value, name == "sssklv2.orders.created"),
    RevenueEUR = round(sumif(value, name == "sssklv2.orders.revenue"), 2)
    by Product
| order by RevenueEUR desc
```

### Busiest hours of the day

The `hourofday` function uses UTC; the offset converts it to Dutch time. Use `1h` in winter and `2h` in summer.

```kusto
customMetrics
| where timestamp > ago(30d)
| where name == "sssklv2.orders.created"
| summarize Orders = sum(value) by Hour = hourofday(timestamp + 2h)
| order by Hour asc
| render columnchart
```

### Top-ups per day

```kusto
customMetrics
| where timestamp > ago(30d)
| where name in ("sssklv2.topups.created", "sssklv2.topups.amount")
| summarize
    TopUps = sumif(value, name == "sssklv2.topups.created"),
    AmountEUR = round(sumif(value, name == "sssklv2.topups.amount"), 2)
    by Day = bin(timestamp, 1d)
| order by Day desc
```

### Kiosk activity in the last hour

```kusto
customMetrics
| where timestamp > ago(1h)
| where name == "sssklv2.orders.created"
| extend Product = tostring(customDimensions["product"])
| summarize Orders = sum(value) by Product, bin(timestamp, 5m)
| render timechart
```

---

## Reliability

### Side-effect failures by source

These are failures after an order or top-up was already saved: the live purchase broadcast, on-behalf notifications and domain event handlers.

```kusto
customMetrics
| where timestamp > ago(7d)
| where name == "sssklv2.side_effects.failed"
| extend Source = tostring(customDimensions["source"])
| summarize Failures = sum(value) by Source, Day = bin(timestamp, 1d)
| order by Day desc, Failures desc
```

Details, with exception:

```kusto
traces
| where timestamp > ago(7d)
| where message has "failed after order was saved" or message startswith "Domain event handler"
| project timestamp, message, operation_Id
| join kind=leftouter (exceptions | project operation_Id, type, outerMessage) on operation_Id
| project timestamp, message, type, outerMessage
| order by timestamp desc
```

### Web push delivery outcomes

```kusto
customMetrics
| where timestamp > ago(7d)
| where name == "sssklv2.push.sent"
| extend Outcome = tostring(customDimensions["outcome"])
| summarize Count = sum(value) by Outcome, Day = bin(timestamp, 1d)
| order by Day desc
```

Push errors per user:

```kusto
traces
| where timestamp > ago(7d)
| where message has "push notification to user"
| extend UserId = tostring(customDimensions.UserId), StatusCode = tostring(customDimensions.StatusCode)
| summarize Errors = count(), LastSeen = max(timestamp) by UserId, StatusCode
| order by Errors desc
```

### Background jobs (CSV export, stats recalculation)

```kusto
customMetrics
| where timestamp > ago(30d)
| where name == "sssklv2.jobs.duration"
| extend Job = tostring(customDimensions["job"]), Outcome = tostring(customDimensions["outcome"])
| summarize Runs = sum(valueCount), AvgSeconds = round(sum(valueSum) / sum(valueCount), 1), MaxSeconds = max(valueMax)
    by Job, Outcome
```

Job runs as spans, with failure details:

```kusto
dependencies
| where timestamp > ago(30d)
| where name in ("OrderCsvExportJob", "StatsRecalculationJob")
| project timestamp, name, duration, success, JobId = tostring(customDimensions["job.id"]), operation_Id
| order by timestamp desc
```

```kusto
traces
| where timestamp > ago(30d)
| where message has "job" and severityLevel >= 3
| project timestamp, message, JobId = tostring(customDimensions.JobId)
| order by timestamp desc
```

### Rate limiting (sign-in and email endpoints)

```kusto
traces
| where timestamp > ago(7d)
| where message startswith "Rate limit hit"
| extend Path = tostring(customDimensions.Path), RemoteIp = tostring(customDimensions.RemoteIp)
| summarize Hits = count(), LastSeen = max(timestamp) by Path, RemoteIp
| order by Hits desc
```

### Turnstile (bot protection) problems

```kusto
traces
| where timestamp > ago(7d)
| where message startswith "Turnstile"
| summarize Count = count(), LastSeen = max(timestamp), Sample = any(message) by severityLevel, Template = substring(message, 0, 40)
| order by Count desc
```

### Email (SMTP) failures

```kusto
traces
| where timestamp > ago(7d)
| where severityLevel >= 2
| where message has_any ("SMTP", "Email settings", "sending email", "Test email")
| project timestamp, severityLevel, message
| order by timestamp desc
```

---

## Dependencies

### Slow or failing SQL

```kusto
dependencies
| where timestamp > ago(24h)
| where type has "SQL"
| summarize Calls = count(), Failed = countif(success == false), P95ms = percentile(duration, 95) by target, name
| order by P95ms desc
| take 20
```

### Outgoing HTTP and blob storage

```kusto
dependencies
| where timestamp > ago(24h)
| where type !has "SQL" and type != "InProc"
| summarize Calls = count(), Failed = countif(success == false), P95ms = percentile(duration, 95) by type, target
| order by Failed desc, Calls desc
```

---

## Runtime

### Memory and GC

Lists the .NET runtime metrics that are available, with recent averages. Pick a name for a chart.

```kusto
customMetrics
| where timestamp > ago(1h)
| where name startswith "dotnet." or name startswith "process."
| summarize Avg = avg(value), Max = max(value) by name
| order by name asc
```

```kusto
customMetrics
| where timestamp > ago(24h)
| where name == "dotnet.process.memory.working_set"
| summarize WorkingSetMB = avg(value) / 1024 / 1024 by bin(timestamp, 15m)
| render timechart
```

---

## Alert suggestions

These queries are good candidates for log alert rules (**Alerts → Create → Alert rule → Custom log search**):

| Alert | Query | Condition |
|---|---|---|
| Server errors | `requests \| where toint(resultCode) >= 500` | more than 5 rows in 15 minutes |
| Side-effect failures | `customMetrics \| where name == "sssklv2.side_effects.failed" \| summarize sum(value)` | sum above 0 in 1 hour |
| Failed background job | `customMetrics \| where name == "sssklv2.jobs.duration" and tostring(customDimensions["outcome"]) == "failed"` | at least 1 row |
| Turnstile key rejected | `traces \| where message has "Turnstile secret key"` | at least 1 row |
| MediatR license | `traces \| where message has "MediatR" and message has "licens"` | at least 1 row in 1 day |
