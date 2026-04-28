# Phase 1: Backend API — Discussion Log

**Date:** 2026-04-28
**Status:** Complete → CONTEXT.md written

---

## Areas Discussed

### 1. Error Handling

| Question | Options Presented | Selected |
|----------|------------------|----------|
| What should happen when Open-Meteo is unreachable? | 503 with error message / Serve stale cache / Return empty array | **HTTP 503 with clear error message** |
| Should the backend retry before returning 503? | No retries / Retry once / Retry up to 3× exponential backoff | **Retry up to 3 times with exponential backoff** |
| What should the 503 response body look like? | Generic message / Exception message / Structured `{ "error", "message" }` | **Structured JSON: `{ "error": "upstream_failure", "message": "..." }`** |

### 2. Old Controller Cleanup

| Question | Options Presented | Selected |
|----------|------------------|----------|
| What to do with WeatherForecastController? | Remove it / Keep it / Rename/move | **Remove entirely** |

### 3. City Names Language

| Question | Options Presented | Selected |
|----------|------------------|----------|
| What language for the `name` field in API response? | Bulgarian Cyrillic / Latin transliteration / Both (nameNative + nameEn) | **Both — `nameNative` (Cyrillic) + `nameEn` (Latin)** |

### 4. Positional Join Safety

| Question | Options Presented | Selected |
|----------|------------------|----------|
| What if Open-Meteo returns fewer than 28 items? | Validate → 503 / Trust → join by index silently / Log warning + proceed | **Log a warning but proceed with partial data** |

---

## Agent Discretion

- Exact exponential backoff intervals
- HttpClient timeout value
- Exact 503 error message text
- Logging verbosity for cache hits/misses

---

## Deferred Ideas

None.
