# Phase 1: Backend API - Pattern Map

**Mapped:** 2026-04-28
**Files analyzed:** 10 (8 new, 1 modify, 1 delete)
**Analogs found:** 8 / 8 (all new/modified files have at least a partial analog)

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `Data/BulgarianCities.cs` | data/constant | none (static) | `Utilities/StringShortener.cs` | structural (static class, file-scoped ns) |
| `Models/CityTemperature.cs` | model/DTO | request-response | `WeatherForecast.cs` | exact (model with nullable props) |
| `Models/OpenMeteoResponse.cs` | model/DTO | request-response | `WeatherForecast.cs` | exact (model with nullable props) |
| `Services/IOpenMeteoClient.cs` | interface | request-response | `WeatherForecast.cs` | partial (namespace + file-scoped ns pattern) |
| `Services/OpenMeteoClient.cs` | service | request-response | `Controllers/WeatherForecastController.cs` | partial (DI injection, ILogger) |
| `Services/ICityTemperatureService.cs` | interface | CRUD | `WeatherForecast.cs` | partial (namespace pattern) |
| `Services/CityTemperatureService.cs` | service | CRUD + cache | `Controllers/WeatherForecastController.cs` | partial (DI injection, ILogger) |
| `Controllers/CitiesController.cs` | controller | request-response | `Controllers/WeatherForecastController.cs` | exact (ApiController, Route, ILogger DI) |
| `Program.cs` *(modify)* | config | — | `Program.cs` | exact (same file) |
| `Controllers/WeatherForecastController.cs` *(delete)* | — | — | — | — |

---

## Pattern Assignments

### `Data/BulgarianCities.cs` (data class, static)

**Analog:** `Utilities/StringShortener.cs`

**Namespace + file-scoped pattern** (line 5):
```csharp
namespace WeatherForecast.Utilities;
```
> CONTEXT.md overrides: use namespace `WeatherForecast` (flat). Apply file-scoped namespace syntax:
```csharp
namespace WeatherForecast;
```

**Static class pattern** (`StringShortener.cs` lines 12–15):
```csharp
public static partial class StringShortener
{
    private const int MaxLength = 64;
```
> Adapt to a non-partial static class holding a `static readonly` list:
```csharp
public static class BulgarianCities
{
    public static readonly IReadOnlyList<CityInfo> All = new[]
    {
        new CityInfo("София",     "Sofia",    42.6977, 23.3219),
        // ... 27 more
    }.AsReadOnly();
}

public record CityInfo(string NameNative, string NameEn, double Latitude, double Longitude);
```

**No auth, no error handling** — pure compile-time constant data.

---

### `Models/CityTemperature.cs` (model/DTO, request-response)

**Analog:** `WeatherForecast.cs`

**Namespace + class pattern** (`WeatherForecast.cs` lines 1–13):
```csharp
namespace WeatherForecast
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC * 9.0 / 5.0);
        public string? Summary { get; set; }
    }
}
```
> Key observations:
> - Block-scoped namespace used in this file — but CONTEXT.md mandates **file-scoped namespaces** for new files (align with `StringShortener.cs` style)
> - Nullable reference types enabled (`<Nullable>enable</Nullable>` in `.csproj`) — use `?` on reference types
> - ImplicitUsings enabled — no `using System;` needed

**Adapt to DTO shape** (D-05, D-07):
```csharp
namespace WeatherForecast;

public class CityTemperature
{
    public string NameNative { get; set; } = string.Empty;
    public string NameEn     { get; set; } = string.Empty;
    public double Latitude   { get; set; }
    public double Longitude  { get; set; }
    public double TemperatureC { get; set; }
}
```

---

### `Models/OpenMeteoResponse.cs` (model/DTO, deserialization)

**Analog:** `WeatherForecast.cs`

**Nullable properties pattern** (`WeatherForecast.cs` line 11):
```csharp
public string? Summary { get; set; }
```
> PITFALLS.md #10 — nullable DTO required because `<Nullable>enable</Nullable>` is on and Open-Meteo may return missing fields:

```csharp
namespace WeatherForecast;

using System.Text.Json.Serialization;

public class OpenMeteoResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("current")]
    public OpenMeteoCurrent? Current { get; set; }   // nullable per PITFALLS #10
}

public class OpenMeteoCurrent
{
    [JsonPropertyName("temperature_2m")]
    public double Temperature2M { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; }
}
```
> `System.Text.Json` with `[JsonPropertyName]` attributes — no Newtonsoft. STACK.md confirms `JsonNamingPolicy.SnakeCaseLower` is an alternative but explicit attributes are clearer for this small model.

---

### `Services/IOpenMeteoClient.cs` (interface, request-response)

**Analog:** Namespace/file structure from `WeatherForecast.cs`

**No direct interface analog exists in the codebase.** Use standard ASP.NET Core interface pattern:

```csharp
namespace WeatherForecast;

public interface IOpenMeteoClient
{
    Task<IReadOnlyList<OpenMeteoResponse>> GetCurrentTemperaturesAsync(
        IReadOnlyList<CityInfo> cities,
        CancellationToken cancellationToken = default);
}
```
> `CancellationToken` parameter per PITFALLS.md #9 (allows ASP.NET Core to cancel on browser nav-away).

---

### `Services/OpenMeteoClient.cs` (service, request-response + retry)

**Analog:** `Controllers/WeatherForecastController.cs`

**Constructor DI injection pattern** (`WeatherForecastController.cs` lines 14–18):
```csharp
private readonly ILogger<WeatherForecastController> _logger;

public WeatherForecastController(ILogger<WeatherForecastController> logger)
{
    _logger = logger;
}
```
> Adapt: inject `IHttpClientFactory` and `ILogger<OpenMeteoClient>`. Use named client `"open-meteo"`.

**Core service pattern** — build on the DI pattern above:
```csharp
namespace WeatherForecast;

public class OpenMeteoClient : IOpenMeteoClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenMeteoClient> _logger;

    public OpenMeteoClient(IHttpClientFactory httpClientFactory, ILogger<OpenMeteoClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OpenMeteoResponse>> GetCurrentTemperaturesAsync(
        IReadOnlyList<CityInfo> cities,
        CancellationToken cancellationToken = default)
    {
        var lats = string.Join(",", cities.Select(c => c.Latitude));
        var lons = string.Join(",", cities.Select(c => c.Longitude));
        var url  = $"/v1/forecast?latitude={lats}&longitude={lons}&current=temperature_2m&forecast_days=1";

        // Retry loop: D-01 (3 attempts, exponential backoff)
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var client   = _httpClientFactory.CreateClient("open-meteo");
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var result = await response.Content
                    .ReadFromJsonAsync<IReadOnlyList<OpenMeteoResponse>>(cancellationToken: cancellationToken);
                return result ?? [];
            }
            catch (Exception ex) when (attempt < 3)
            {
                _logger.LogWarning(ex, "Open-Meteo attempt {Attempt} failed. Retrying...", attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(3, attempt - 1)), cancellationToken);
            }
        }
        // Final attempt (no catch — let exception propagate to service)
        var finalClient   = _httpClientFactory.CreateClient("open-meteo");
        var finalResponse = await finalClient.GetAsync(url, cancellationToken);
        finalResponse.EnsureSuccessStatusCode();
        var finalResult = await finalResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<OpenMeteoResponse>>(cancellationToken: cancellationToken);
        return finalResult ?? [];
    }
}
```
> Backoff intervals at discretion (CONTEXT.md): 100ms → 300ms → 900ms (×3 exponent).
> IHttpClientFactory pattern is critical — PITFALLS.md #1 (socket exhaustion).

---

### `Services/ICityTemperatureService.cs` (interface, CRUD)

**Analog:** Namespace pattern from `WeatherForecast.cs`

```csharp
namespace WeatherForecast;

public interface ICityTemperatureService
{
    Task<IReadOnlyList<CityTemperature>> GetAllAsync(CancellationToken cancellationToken = default);
}
```

---

### `Services/CityTemperatureService.cs` (service, CRUD + IMemoryCache)

**Analog:** `Controllers/WeatherForecastController.cs` (DI injection pattern)

**Constructor DI pattern** (`WeatherForecastController.cs` lines 14–18):
```csharp
private readonly ILogger<WeatherForecastController> _logger;

public WeatherForecastController(ILogger<WeatherForecastController> logger)
{
    _logger = logger;
}
```
> Extend: inject `IOpenMeteoClient`, `IMemoryCache`, `ILogger<CityTemperatureService>`.

**Cache + join service pattern** (from ARCHITECTURE.md + PITFALLS.md):
```csharp
namespace WeatherForecast;

using Microsoft.Extensions.Caching.Memory;

public class CityTemperatureService : ICityTemperatureService
{
    private const string CacheKey = "bg-city-temps";
    private readonly IOpenMeteoClient _openMeteoClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CityTemperatureService> _logger;

    public CityTemperatureService(
        IOpenMeteoClient openMeteoClient,
        IMemoryCache cache,
        ILogger<CityTemperatureService> logger)
    {
        _openMeteoClient = openMeteoClient;
        _cache           = cache;
        _logger          = logger;
    }

    public async Task<IReadOnlyList<CityTemperature>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<CityTemperature>? cached))
        {
            _logger.LogDebug("Cache HIT for {CacheKey}", CacheKey);
            return cached!;
        }

        _logger.LogInformation("Cache MISS for {CacheKey}. Fetching from Open-Meteo.", CacheKey);
        var cities    = BulgarianCities.All;
        var responses = await _openMeteoClient.GetCurrentTemperaturesAsync(cities, cancellationToken);

        // D-08: partial response — zip by index, log warning if fewer than 28
        if (responses.Count < cities.Count)
            _logger.LogWarning("Open-Meteo returned {Actual} items; expected {Expected}. Proceeding with partial data.",
                responses.Count, cities.Count);

        var result = cities
            .Zip(responses, (city, r) => new CityTemperature
            {
                NameNative   = city.NameNative,
                NameEn       = city.NameEn,
                Latitude     = city.Latitude,
                Longitude    = city.Longitude,
                TemperatureC = r.Current?.Temperature2M ?? double.NaN  // PITFALLS #10 null-safe
            })
            .ToList()
            .AsReadOnly();

        _cache.Set(CacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }
}
```

---

### `Controllers/CitiesController.cs` (controller, request-response)

**Analog:** `Controllers/WeatherForecastController.cs` — **exact pattern match**

**Full analog** (`WeatherForecastController.cs` lines 1–33):
```csharp
using Microsoft.AspNetCore.Mvc;

namespace WeatherForecast.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            ...
        }
    }
}
```

**Adapt — `CitiesController` pattern:**
```csharp
using Microsoft.AspNetCore.Mvc;

namespace WeatherForecast;   // flat namespace per CONTEXT.md

[ApiController]
[Route("api/cities")]        // NOTE: literal route, not [controller] token (CONTEXT.md)
public class CitiesController : ControllerBase
{
    private readonly ICityTemperatureService _service;
    private readonly ILogger<CitiesController> _logger;

    public CitiesController(ICityTemperatureService service, ILogger<CitiesController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    [HttpGet("temperatures")]
    public async Task<IActionResult> GetTemperatures(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetAllAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve city temperatures after all retries.");
            return StatusCode(503, new
            {
                error   = "upstream_failure",
                message = "Could not retrieve temperature data from Open-Meteo. Please try again later."
            });
        }
    }
}
```
> Route: `[Route("api/cities")]` + `[HttpGet("temperatures")]` → `/api/cities/temperatures` (CONTEXT.md).
> Error response: D-02 structured JSON `{ error, message }` with HTTP 503.
> `async Task<IActionResult>` pattern (upgrade from sync `IEnumerable<T>` in the analog).

---

### `Program.cs` *(modify)* (config, DI registration)

**Analog:** `Program.cs` itself — **same file**

**Existing DI block** (`Program.cs` lines 10–26):
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

**Existing middleware block** (`Program.cs` lines 27–45):
```csharp
var app = builder.Build();

app.UseCors("AllowReactDev");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();   // ← MUST be guarded (PITFALLS #4)

app.UseAuthorization();
app.MapControllers();
app.Run();
```

**Required changes** (minimum diff):
```csharp
// ADD after AddSwaggerGen():
builder.Services.AddHttpClient("open-meteo", client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(10);   // PITFALLS #9
});
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IOpenMeteoClient, OpenMeteoClient>();
builder.Services.AddScoped<ICityTemperatureService, CityTemperatureService>();

// CHANGE UseHttpsRedirection (PITFALLS #4, CONTEXT.md API-05):
// BEFORE:
app.UseHttpsRedirection();
// AFTER:
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
```

---

## Shared Patterns

### Namespace Convention
**Source:** `Utilities/StringShortener.cs` line 5 AND CONTEXT.md "Established Patterns"
**Apply to:** All new `.cs` files

```csharp
// File-scoped namespace syntax (C# 10+), flat — no sub-namespaces
namespace WeatherForecast;
```
> Exception: The existing `WeatherForecastController.cs` uses block-scoped `namespace WeatherForecast.Controllers { }`.
> CONTEXT.md is explicit: "Namespace: `WeatherForecast` (flat, no sub-namespaces)".
> All **new** files use `namespace WeatherForecast;` (file-scoped, flat).

---

### No Explicit `using` for BCL / ASP.NET
**Source:** `WeatherForecast.csproj` lines 5–6
**Apply to:** All new `.cs` files

```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```
> `ImplicitUsings` covers `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`, `Microsoft.AspNetCore.Mvc`, etc.
> Only add `using` statements for things NOT in the implicit set (e.g., `Microsoft.Extensions.Caching.Memory`, `System.Text.Json.Serialization`).

---

### Nullable Reference Types — Always Use on Reference Properties
**Source:** `WeatherForecast.cs` line 11; `WeatherForecast.csproj` line 5
**Apply to:** `Models/OpenMeteoResponse.cs`, `Models/CityTemperature.cs`, any service returning nullable values

```csharp
public string? Summary { get; set; }          // existing pattern
public OpenMeteoCurrent? Current { get; set; } // adapt for OpenMeteoResponse
```
> `<Nullable>enable</Nullable>` means the compiler warns on unguarded dereferences.
> PITFALLS.md #10: always null-check Open-Meteo deserialized objects before accessing nested properties.

---

### ILogger DI Injection
**Source:** `Controllers/WeatherForecastController.cs` lines 14–18
**Apply to:** `OpenMeteoClient.cs`, `CityTemperatureService.cs`, `CitiesController.cs`

```csharp
private readonly ILogger<WeatherForecastController> _logger;

public WeatherForecastController(ILogger<WeatherForecastController> logger)
{
    _logger = logger;
}
```
> Replace generic type argument with the concrete class being logged from.

---

### Error Response Shape (503)
**Source:** CONTEXT.md D-02; no existing analog in codebase
**Apply to:** `CitiesController.cs` catch block only

```csharp
return StatusCode(503, new
{
    error   = "upstream_failure",
    message = "Could not retrieve temperature data from Open-Meteo. Please try again later."
});
```

---

### Retry + Exponential Backoff
**Source:** CONTEXT.md D-01, D-03; no existing analog in codebase
**Apply to:** `OpenMeteoClient.cs` only

```csharp
// Intervals: 100ms → 300ms → 900ms (×3 exponent, 3 attempts max — D-01)
await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(3, attempt - 1)), cancellationToken);
// Log at each attempt (D-03)
_logger.LogWarning(ex, "Open-Meteo attempt {Attempt} failed. Retrying...", attempt);
// Log on final failure (D-03) — handled in CitiesController catch block
_logger.LogError(ex, "Failed to retrieve city temperatures after all retries.");
```

---

## No Analog Found

All files have at least a structural analog. The following files have **no functional analog** (logic is entirely new to this codebase) and must draw on ARCHITECTURE.md / PITFALLS.md / STACK.md exclusively for their implementation logic:

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `Services/IOpenMeteoClient.cs` | interface | request-response | No interfaces exist in the codebase |
| `Services/ICityTemperatureService.cs` | interface | CRUD | No interfaces exist in the codebase |
| `Services/OpenMeteoClient.cs` | service | request-response | No HttpClient services exist in the codebase |
| `Services/CityTemperatureService.cs` | service | CRUD + cache | No cache services exist in the codebase |

> Planner should reference ARCHITECTURE.md §"ASP.NET Core Registration" and PITFALLS.md #1, #2, #3, #9, #10 for these files.

---

## Metadata

**Analog search scope:** `src/WeatherForecast/` (all `.cs` files)
**Files scanned:** 5 (`WeatherForecastController.cs`, `WeatherForecast.cs`, `Program.cs`, `StringShortener.cs`, `WeatherForecast.csproj`)
**Pattern extraction date:** 2026-04-28
