---
status: complete
phase: 01-backend-api
source: 01-01-SUMMARY.md, 01-02-SUMMARY.md, 01-03-SUMMARY.md
started: 2026-04-28T09:14:28Z
updated: 2026-04-28T09:59:41Z
---

## Current Test

[testing complete]

## Tests

### 1. Cold Start Smoke Test
expected: Kill any running instance. Run `dotnet run --launch-profile http` from src/WeatherForecast. Server starts without errors and logs `Now listening on: http://localhost:5055`. A curl to http://localhost:5055/api/cities/temperatures returns a JSON response (not a 500 or connection error).
result: pass

### 2. Endpoint Returns Exactly 28 Items
expected: `curl http://localhost:5055/api/cities/temperatures` returns a JSON array with exactly 28 objects — one per Bulgarian district administrative center.
result: pass

### 3. Response JSON Shape
expected: Each object in the array has 5 fields: `nameNative` (city name in Cyrillic, e.g. "София"), `nameEn` (Latin transliteration, e.g. "Sofia"), `latitude` (decimal number), `longitude` (decimal number), and `temperatureC` (decimal number or null).
result: pass

### 4. Real Temperature Data
expected: The `temperatureC` values in the response are real current temperatures sourced live from Open-Meteo — they are non-zero decimal numbers that look like plausible temperatures for Bulgarian cities right now (not all identical, not all 0, not null for most cities).
result: pass

### 5. Caching — Repeated Calls Return Same Data
expected: Call the endpoint twice within 1 minute. The second response returns identical data and arrives noticeably faster (sub-10 ms vs. the first call). Alternatively, check the server logs — second call should log "Cache hit" and the first "Cache miss" (or similar).
result: pass

### 6. HTTPS Redirect Guard in Development
expected: `curl -v http://localhost:5055/api/cities/temperatures` returns HTTP 200 directly — no 301/302 redirect to https://. In development mode the app must not force HTTPS.
result: pass

### 7. Legacy Endpoint Removed
expected: `curl -o /dev/null -w "%{http_code}" http://localhost:5055/weatherforecast` returns 404. The old WeatherForecastController was deleted as part of this phase.
result: pass

## Summary

total: 7
passed: 7
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

[none yet]
