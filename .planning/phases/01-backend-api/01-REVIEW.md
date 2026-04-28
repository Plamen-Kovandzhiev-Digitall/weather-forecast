---
phase: 1
plan: 01
depth: standard
files_reviewed: 9
files_reviewed_list:
  - src/WeatherForecast/Data/BulgarianCities.cs
  - src/WeatherForecast/Models/CityTemperature.cs
  - src/WeatherForecast/Models/OpenMeteoResponse.cs
  - src/WeatherForecast/Services/IOpenMeteoClient.cs
  - src/WeatherForecast/Services/OpenMeteoClient.cs
  - src/WeatherForecast/Services/ICityTemperatureService.cs
  - src/WeatherForecast/Services/CityTemperatureService.cs
  - src/WeatherForecast/Controllers/CitiesController.cs
  - src/WeatherForecast/Program.cs
status: findings
findings:
  critical: 2
  warning: 2
  info: 2
  total: 6
---

# Code Review: Phase 1 — Backend API

## Summary

Nine files reviewed across the full request path: data layer → HTTP client → service → controller → wiring. The architecture is sound and the code is well-structured with good XML documentation, consistent use of `ConfigureAwait(false)`, nullable reference types, and primary constructor syntax.

Two critical bugs require fixes before the service can ship correctly:

1. **`double.NaN` is serialized at the HTTP boundary** — System.Text.Json's default configuration throws `JsonException` on `NaN`. The exception fires *after* the controller's try-catch returns, so the 503 fallback never fires; users see a 500 instead, and the error is untrapped.
2. **Coordinates are formatted with the process culture** — `double.ToString()` in `string.Join` uses `CultureInfo.CurrentCulture`. On any system with a comma decimal separator (e.g., a Docker image or host with `bg-BG` locale, which is the expected deployment target), the Open-Meteo URL is malformed for every call.

Two warnings relate to improper `OperationCanceledException` handling — client disconnects produce spurious error-level log noise and incorrect log messages. Two info items cover dead middleware and a fragile retry-loop layout.

---

## Findings

### CR-001: `double.NaN` Causes Untrapped `JsonException` at the HTTP Boundary *(Critical)*

**File:** `src/WeatherForecast/Services/CityTemperatureService.cs` — line 48  
**Issue:** When Open-Meteo omits the `current` block for a city (documented as possible — see `OpenMeteoResponse.cs` line 20), the fallback value is `double.NaN`:

```csharp
TemperatureC = r.Current?.Temperature2M ?? double.NaN,
```

System.Text.Json **does not support NaN by default** and throws `JsonException: The value 'NaN' is not a supported number value according to the JSON specification.` This exception is raised during response serialization inside `ObjectResult.ExecuteResultAsync`, *which runs after the controller action method has already returned*. Consequently:

- The `try/catch` in `CitiesController.GetTemperatures` **cannot catch it**.
- ASP.NET Core's exception handler returns HTTP 500, not the intended 503.
- The structured error body `{ error, message }` is never written.

**Impact:** Any partial Open-Meteo response (null `Current` for one or more cities) crashes the entire response with an unhandled 500. Swagger UI will also fail to round-trip because `double TemperatureC` does not express the possibility of missing data.

**Fix (option A — nullable DTO property, recommended):**

In `CityTemperature.cs` line 22, change the property type:
```csharp
// Before
public double TemperatureC { get; set; }

// After
public double? TemperatureC { get; set; }
```

Then update the mapping in `CityTemperatureService.cs` line 48:
```csharp
TemperatureC = r.Current?.Temperature2M,   // null when Current is absent
```

**Fix (option B — configure JSON options globally):**

In `Program.cs`, configure the serializer to emit `"NaN"` as a JSON string token (valid per JSON5 but not strict JSON):
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.NumberHandling =
            System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals);
```
Option A is preferable because `null` is idiomatic JSON for "not available" and avoids client-side NaN parsing.

---

### CR-002: Culture-Sensitive `double.ToString()` Produces Malformed Open-Meteo URLs *(Critical)*

**File:** `src/WeatherForecast/Services/OpenMeteoClient.cs` — lines 18–19  
**Issue:** Coordinate lists are built with:

```csharp
var lats = string.Join(",", cities.Select(c => c.Latitude));
var lons = string.Join(",", cities.Select(c => c.Longitude));
```

`string.Join` calls `double.ToString()` (equivalent) on each value using `CultureInfo.CurrentCulture`. On any system where the decimal separator is a comma — including Windows hosts or Docker images configured with a Bulgarian (`bg-BG`) locale, which is the stated deployment target — the coordinate `42.0147` is serialized as `42,0147`. The resulting URL fragment becomes:

```
?latitude=42,0147,42,5048,43,2048,...
```

This is silently passed to Open-Meteo, which either rejects the request or mis-parses all 28 coordinates. Every API call fails for the lifetime of the process.

**Impact:** Complete, silent service failure on culture-sensitive hosts. The bug does not surface in CI unless the test runner is configured with the deployment locale.

**Fix:** Use `CultureInfo.InvariantCulture` explicitly:

```csharp
using System.Globalization;

var lats = string.Join(",", cities.Select(c => c.Latitude.ToString(CultureInfo.InvariantCulture)));
var lons = string.Join(",", cities.Select(c => c.Longitude.ToString(CultureInfo.InvariantCulture)));
```

Alternatively, use a format string: `c.Latitude.ToString("G", CultureInfo.InvariantCulture)`.

---

### WR-001: `OperationCanceledException` Is Silently Swallowed in the Retry Loop *(Warning)*

**File:** `src/WeatherForecast/Services/OpenMeteoClient.cs` — lines 37–46  
**Issue:** The catch clause is `catch (Exception ex)`, which intercepts `OperationCanceledException` (and its subtype `TaskCanceledException`). When a client disconnects and ASP.NET Core cancels the request token, the sequence is:

1. `client.GetAsync(url, cancellationToken)` throws `OperationCanceledException`.
2. The catch block executes, logging a **spurious warning** ("Open-Meteo attempt 1 of 3 failed").
3. `Task.Delay(..., cancellationToken)` is immediately called with an already-cancelled token, throwing `OperationCanceledException` again — this time propagating out of the catch block and up to the caller.

The net result is one false "retry" log entry per cancellation, plus unexpected delay-call behavior. If the token happens not to be cancelled yet at the delay site (race condition), the loop will make a second HTTP attempt against a cancelled request context.

**Fix:** Re-throw `OperationCanceledException` before the generic catch:

```csharp
catch (OperationCanceledException)
{
    throw;   // Honour cancellation immediately — not a retriable failure
}
catch (Exception ex)
{
    var delayMs = (int)(100 * Math.Pow(3, attempt - 1));
    logger.LogWarning(ex,
        "Open-Meteo attempt {Attempt} of 3 failed. Retrying in {DelayMs}ms.",
        attempt, delayMs);
    await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken)
        .ConfigureAwait(false);
}
```

---

### WR-002: Controller Catch-All Converts Client Cancellations into Spurious 503 Log Errors *(Warning)*

**File:** `src/WeatherForecast/Controllers/CitiesController.cs` — lines 33–41  
**Issue:** `catch (Exception ex)` intercepts `OperationCanceledException` raised when the client navigates away. For every client disconnect, the code:

1. Logs at **Error** level: `"Failed to retrieve city temperatures from Open-Meteo."` — a false statement that will pollute dashboards/alerting.
2. Attempts to write a 503 response body to a socket that is already closed.

This conflates a normal client event with a genuine upstream failure, making the logs unreliable.

**Fix:** Add a specific handler for cancellation before the generic catch:

```csharp
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    // Client disconnected — not an error; return no content (connection already closed)
    return StatusCode(499);  // Nginx convention for "client closed request", or simply return;
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to retrieve city temperatures from Open-Meteo.");
    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
    {
        error   = "upstream_failure",
        message = "Could not retrieve temperature data from Open-Meteo. Please try again later.",
    });
}
```

---

### IN-001: `UseAuthorization()` Is Dead Middleware — No Auth Scheme Configured *(Info)*

**File:** `src/WeatherForecast/Program.cs` — line 54  
**Issue:** `app.UseAuthorization()` is registered in the pipeline even though no authentication scheme, no `[Authorize]` attributes, and no authorization policies exist. This is boilerplate left over from the ASP.NET Core Web API template. It adds a small overhead to every request and creates noise if a developer later searches for auth configuration.

**Fix:** Remove the line:

```csharp
// Remove:
app.UseAuthorization();
```

If authorization is added in a future phase, both `app.UseAuthentication()` and `app.UseAuthorization()` should be added together in the correct order.

---

### IN-002: Retry Loop Is Split Across Loop Body and Post-Loop Code — Fragile Structure *(Info)*

**File:** `src/WeatherForecast/Services/OpenMeteoClient.cs` — lines 24–57  
**Issue:** The "3 attempts" design is implemented as a `for` loop covering attempts 1–2, then a duplicate block outside the loop for attempt 3 (the "final attempt"). This works, but:

- The implementation of a single attempt is duplicated in two places (lines 29–35 and lines 51–57). Any change to request logic (headers, deserialization options, etc.) must be applied in both places.
- The loop bound `attempt <= 2` is not self-explanatory; the comment is the only way to understand the intent.
- It is easy for a maintainer to change `<= 2` to `<= 3` (natural-looking fix), which would silently produce 4 total attempts.

**Fix:** Collapse all attempts into a single loop and re-throw on the last failure:

```csharp
const int MaxAttempts = 3;
Exception? lastEx = null;

for (int attempt = 1; attempt <= MaxAttempts; attempt++)
{
    try
    {
        var client = httpClientFactory.CreateClient("open-meteo");
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OpenMeteoResponse>>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return result ?? [];
    }
    catch (OperationCanceledException)
    {
        throw;
    }
    catch (Exception ex)
    {
        lastEx = ex;
        if (attempt < MaxAttempts)
        {
            var delayMs = (int)(100 * Math.Pow(3, attempt - 1));
            logger.LogWarning(ex,
                "Open-Meteo attempt {Attempt} of {Max} failed. Retrying in {DelayMs}ms.",
                attempt, MaxAttempts, delayMs);
            await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

// All attempts exhausted — propagate to controller (returns 503)
ExceptionDispatchInfo.Throw(lastEx!);
return []; // unreachable, satisfies compiler
```

(Requires `using System.Runtime.ExceptionServices;`)

---

## Files Reviewed

| File | Status |
|------|--------|
| `src/WeatherForecast/Data/BulgarianCities.cs` | ✓ Clean |
| `src/WeatherForecast/Models/CityTemperature.cs` | ⚠ 1 finding (CR-001 impact) |
| `src/WeatherForecast/Models/OpenMeteoResponse.cs` | ✓ Clean |
| `src/WeatherForecast/Services/IOpenMeteoClient.cs` | ✓ Clean |
| `src/WeatherForecast/Services/OpenMeteoClient.cs` | ⚠ 3 findings (CR-002, WR-001, IN-002) |
| `src/WeatherForecast/Services/ICityTemperatureService.cs` | ✓ Clean |
| `src/WeatherForecast/Services/CityTemperatureService.cs` | ⚠ 1 finding (CR-001) |
| `src/WeatherForecast/Controllers/CitiesController.cs` | ⚠ 1 finding (WR-002) |
| `src/WeatherForecast/Program.cs` | ⚠ 1 finding (IN-001) |

---

_Reviewed: 2025-07-14T00:00:00Z_  
_Reviewer: gsd-code-reviewer (adversarial, standard depth)_  
_Depth: standard_
