---
phase: 1
plan: 01
iteration: 1
fix_scope: critical_warning
findings_in_scope: 4
fixed: 4
skipped: 0
status: all_fixed
---

# Code Review Fix Report: Phase 1 — Backend API

## Summary

All 4 in-scope findings (2 Critical, 2 Warning) were fixed and committed atomically. Each fix
was verified with a clean `dotnet build` before committing.

- **CR-001:** Changed `TemperatureC` from `double` to `double?` in the DTO and removed the
  `?? double.NaN` fallback in the service mapping. System.Text.Json now serialises missing
  temperatures as `null` (idiomatic JSON) rather than throwing an untrapped `JsonException`.
- **CR-002:** Added `using System.Globalization;` and wrapped each coordinate in
  `.ToString(CultureInfo.InvariantCulture)` so the Open-Meteo URL is always well-formed,
  regardless of the host locale.
- **WR-001:** Added `catch (OperationCanceledException) { throw; }` before the generic
  `catch (Exception ex)` in the retry loop so client disconnects are not silently swallowed
  and do not produce spurious retry-log entries.
- **WR-002:** Added `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)`
  before the generic catch in the controller, returning HTTP 499 for client disconnects
  instead of logging a false "upstream failure" error.

## Fixes Applied

### CR-001: `double.NaN` Causes Untrapped `JsonException` at the HTTP Boundary — ✓ Fixed

**Commit:** `d2c5770`  
**Changes:**
- `src/WeatherForecast/Models/CityTemperature.cs` — `public double TemperatureC` → `public double? TemperatureC`; updated XML doc comment to reflect `null` semantics.
- `src/WeatherForecast/Services/CityTemperatureService.cs` — `r.Current?.Temperature2M ?? double.NaN` → `r.Current?.Temperature2M` (null propagates naturally via `?.`).

### CR-002: Culture-Sensitive `double.ToString()` Produces Malformed Open-Meteo URLs — ✓ Fixed

**Commit:** `f6958fe`  
**Changes:**
- `src/WeatherForecast/Services/OpenMeteoClient.cs` — added `using System.Globalization;`; changed both `string.Join` coordinate projections to call `.ToString(CultureInfo.InvariantCulture)` explicitly.

### WR-001: `OperationCanceledException` Is Silently Swallowed in the Retry Loop — ✓ Fixed

**Commit:** `d56b08e`  
**Changes:**
- `src/WeatherForecast/Services/OpenMeteoClient.cs` — inserted `catch (OperationCanceledException) { throw; }` immediately before the `catch (Exception ex)` block inside the retry `for` loop.

### WR-002: Controller Catch-All Converts Client Cancellations into Spurious 503 Log Errors — ✓ Fixed

**Commit:** `4353af3`  
**Changes:**
- `src/WeatherForecast/Controllers/CitiesController.cs` — inserted `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return StatusCode(499); }` before the existing `catch (Exception ex)` block.

## Skipped Findings

None — all in-scope findings were successfully fixed.

## Files Changed

| File | Findings Fixed |
|------|----------------|
| `src/WeatherForecast/Models/CityTemperature.cs` | CR-001 |
| `src/WeatherForecast/Services/CityTemperatureService.cs` | CR-001 |
| `src/WeatherForecast/Services/OpenMeteoClient.cs` | CR-002, WR-001 |
| `src/WeatherForecast/Controllers/CitiesController.cs` | WR-002 |

---

_Fixed: 2025-07-14T00:00:00Z_  
_Fixer: gsd-code-fixer_  
_Iteration: 1_
