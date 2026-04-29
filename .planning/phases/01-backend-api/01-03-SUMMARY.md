---
phase: 1
plan: 01-03
subsystem: controller-wiring
tags: [controller, di, program]
key-files:
  created:
    - src/WeatherForecast/Controllers/CitiesController.cs
  modified:
    - src/WeatherForecast/Program.cs
  deleted:
    - src/WeatherForecast/Controllers/WeatherForecastController.cs
    - src/WeatherForecast/WeatherForecast.cs
---

# Plan 01-03 Summary: Controller, DI Wiring & Cleanup

## What Was Built

1. **`Controllers/CitiesController.cs`** — `[Route("api/cities")]` controller with `GET temperatures` action. Primary constructor DI for `ICityTemperatureService` and `ILogger`. Returns 200 with `IReadOnlyList<CityTemperature>` on success; catches all exceptions and returns structured 503 (`upstream_failure`) per D-02. `ConfigureAwait(false)` on the await call.

2. **`Program.cs` modifications:**
   - Added `builder.Services.AddMemoryCache()`
   - Added named `HttpClient("open-meteo")` with `BaseAddress = https://api.open-meteo.com`, `Timeout = 10s`, `Accept: application/json`
   - Added `builder.Services.AddScoped<IOpenMeteoClient, OpenMeteoClient>()`
   - Added `builder.Services.AddScoped<ICityTemperatureService, CityTemperatureService>()`
   - Wrapped `app.UseHttpsRedirection()` in `if (!app.Environment.IsDevelopment())` guard (PITFALLS #4)
   - Preserved existing `AllowReactDev` CORS policy for `http://localhost:5173`

3. **Deleted legacy files:**
   - `Controllers/WeatherForecastController.cs`
   - `WeatherForecast.cs`

## Commits

| Commit | Description |
|--------|-------------|
| 6127b99 | feat(01-03): add CitiesController, wire DI in Program.cs, remove legacy weather files |

## Deviations

None. Plan executed exactly as specified.

## Self-Check

- [x] `CitiesController` uses `[Route("api/cities")]` + `[HttpGet("temperatures")]`
- [x] Primary constructor DI syntax
- [x] Structured 503 on exception: `{ "error": "upstream_failure", "message": "..." }`
- [x] `ConfigureAwait(false)` on await
- [x] `AddMemoryCache()` registered
- [x] Named `"open-meteo"` client: base address + 10s timeout + Accept header
- [x] Both interfaces registered as `AddScoped<>`
- [x] `UseHttpsRedirection` guarded with `!IsDevelopment()` check
- [x] CORS `AllowReactDev` policy preserved
- [x] Legacy `WeatherForecastController.cs` and `WeatherForecast.cs` deleted
- [x] `dotnet build` exits 0 with 0 errors after deletions

**Self-Check: PASSED**
