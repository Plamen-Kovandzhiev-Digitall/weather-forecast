# Phase 1: Backend API - Context

**Gathered:** 2026-04-28
**Status:** Ready for planning

<domain>
## Phase Boundary

The `/api/cities/temperatures` endpoint is live, returning real Open-Meteo temperatures for all 28 Bulgarian district cities with 10-minute caching and correct CORS headers. No frontend changes in this phase.

</domain>

<decisions>
## Implementation Decisions

### Error Handling
- **D-01:** Retry Open-Meteo requests up to 3 times with exponential backoff before giving up
- **D-02:** On exhausted retries, return HTTP 503 with a structured JSON body: `{ "error": "upstream_failure", "message": "..." }`
- **D-03:** Log the error at each retry attempt and on final failure

### Old Controller Cleanup
- **D-04:** Remove `WeatherForecastController.cs` entirely — it returns random mock data incompatible with the project's purpose. No replacement; only the new `CitiesController` remains.

### City Names in API Response
- **D-05:** The API response includes **both** `nameNative` (Cyrillic, e.g. `"София"`) and `nameEn` (Latin transliteration, e.g. `"Sofia"`) per city object
- **D-06:** `BulgarianCities.cs` stores both name variants as the source of truth
- **D-07:** The DTO shape is: `{ nameNative, nameEn, latitude, longitude, temperatureC }` — this supersedes the single `name` field shown in ARCHITECTURE.md

### Positional Join Safety (Open-Meteo bulk response)
- **D-08:** If Open-Meteo returns fewer than 28 items, log a warning and proceed with the partial data (zip by index using however many items arrived); do not fail the request

### the agent's Discretion
- Exact exponential backoff intervals (e.g. 100ms → 300ms → 900ms)
- HttpClient timeout value (PITFALLS.md recommends 10 seconds — use that as baseline)
- Exact structured error message text in the 503 body
- Logging verbosity for cache hits vs misses

</decisions>

<specifics>
## Specific Ideas

- Retry strategy should use exponential backoff (not fixed delay)
- Error response must be structured JSON (`{ "error": "upstream_failure", "message": "..." }`) to give the frontend a machine-readable signal

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Architecture & Component Design
- `.planning/research/ARCHITECTURE.md` — Full architecture blueprint: components, data flow, build order, Program.cs DI registrations, key decisions (Open-Meteo bulk strategy, IMemoryCache TTL, IHttpClientFactory, coordinate delivery)

### Known Pitfalls (MUST READ)
- `.planning/research/PITFALLS.md` — Critical pitfalls for Phase 1: socket exhaustion (#1), wrong Open-Meteo parameter (#2), positional index join (#3), UseHttpsRedirection breaking HTTP fetch (#4), nullable deserialization crash (#10), missing HttpClient timeout (#9)

### Locked Requirements
- `.planning/REQUIREMENTS.md` §Backend/API — API-01 through API-06: endpoint shape, Open-Meteo bulk call, IMemoryCache 10-min TTL, BulgarianCities.cs, UseHttpsRedirection guard, CORS for localhost:5173

### Project Context
- `.planning/PROJECT.md` — Project goals, constraints, tech stack decisions, out-of-scope items

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/WeatherForecast/Program.cs` — CORS already configured (`AllowReactDev` → `http://localhost:5173`); `UseHttpsRedirection` is present but unguarded (needs `if (!app.Environment.IsDevelopment())` guard per API-05)
- `src/WeatherForecast/Controllers/WeatherForecastController.cs` — Controller pattern with `[ApiController]`, `[Route("[controller]")]`, constructor DI injection; use as structural reference before deleting

### Established Patterns
- Namespace: `WeatherForecast` (flat, no sub-namespaces) — all new files follow this
- Nullable reference types enabled — model all Open-Meteo DTOs with nullable properties
- Implicit usings enabled — no need to add `using System;` etc.

### Integration Points
- `Program.cs` requires new DI registrations: `AddHttpClient("open-meteo", ...)`, `AddMemoryCache()`, `AddScoped<ICityTemperatureService, CityTemperatureService>()`, `AddScoped<IOpenMeteoClient, OpenMeteoClient>()`
- New controller route must be `/api/cities/temperatures` — note the controller base route should be `[Route("api/cities")]` with action `[HttpGet("temperatures")]`
- `UseHttpsRedirection` call in `Program.cs` must be wrapped in `if (!app.Environment.IsDevelopment())`

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 01-backend-api*
*Context gathered: 2026-04-28*
