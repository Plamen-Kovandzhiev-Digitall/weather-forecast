---
phase: 1
plan: 01-02
subsystem: services
tags: [http-client, cache, retry]
key-files:
  created:
    - src/WeatherForecast/Services/IOpenMeteoClient.cs
    - src/WeatherForecast/Services/OpenMeteoClient.cs
    - src/WeatherForecast/Services/ICityTemperatureService.cs
    - src/WeatherForecast/Services/CityTemperatureService.cs
---

# Plan 01-02 Summary: Service Layer — HTTP Client & Cache Service

## What Was Built

Four service files implementing the business logic layer:

1. **`Services/IOpenMeteoClient.cs`** — Interface for the Open-Meteo HTTP client, typed to `IReadOnlyList<OpenMeteoResponse>`. Documents positional ordering guarantee.

2. **`Services/OpenMeteoClient.cs`** — Concrete HTTP client using `IHttpClientFactory.CreateClient("open-meteo")`. Builds a single bulk URL with comma-separated lat/lon. Implements 3-attempt retry with exponential backoff: attempt 1 = 100ms delay, attempt 2 = 300ms delay (formula: `100 * Math.Pow(3, attempt-1)`). All `await` calls use `ConfigureAwait(false)`.

3. **`Services/ICityTemperatureService.cs`** — Interface for the city temperature aggregate service, returning `IReadOnlyList<CityTemperature>`.

4. **`Services/CityTemperatureService.cs`** — Cache wrapper around `IOpenMeteoClient`. Cache key `"bg-city-temps"`, absolute TTL 10 minutes. Positional zip-join via `cities.Zip(responses, ...)`. Null-safe temperature access: `r.Current?.Temperature2M ?? double.NaN`. Logs cache hit/miss and partial response warnings.

## Commits

| Commit | Description |
|--------|-------------|
| 6349b1e | feat(01-02): add service layer — OpenMeteoClient, CityTemperatureService, interfaces |

## Deviations

None. Plan executed exactly as specified.

## Self-Check

- [x] All 4 files created in `Services/` with `namespace WeatherForecast;` (file-scoped)
- [x] Primary constructor syntax throughout
- [x] `ConfigureAwait(false)` on all `await` calls
- [x] PITFALLS #1: `IHttpClientFactory.CreateClient("open-meteo")` — no `new HttpClient()`
- [x] PITFALLS #2: URL uses `current=temperature_2m`
- [x] PITFALLS #3: positional Zip join, not coordinate matching
- [x] PITFALLS #10: null-safe `r.Current?.Temperature2M ?? double.NaN`
- [x] D-01 retry: 100ms/300ms backoff, attempt 3 propagates
- [x] D-08: partial response logged and handled
- [x] API-03: cache TTL `TimeSpan.FromMinutes(10)`
- [x] `dotnet build` exits 0 with 0 errors

**Self-Check: PASSED**
