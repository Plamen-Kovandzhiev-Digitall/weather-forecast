---
phase: 02-frontend-map
plan: 02
subsystem: frontend-types-hooks
tags: [typescript, react, hooks, data-fetching, types]
dependency_graph:
  requires: [02-01]
  provides: [CityTemperature interface, useCityTemperatures hook]
  affects: [02-03-BulgariaMap, 02-04-App]
tech_stack:
  added: []
  patterns: [cancelled-flag cancel-on-unmount, retryCount re-fetch trigger, Array.isArray runtime validation]
key_files:
  created:
    - src/weather-ui/src/types/cityTemperature.ts
    - src/weather-ui/src/hooks/useCityTemperatures.ts
  modified: []
decisions:
  - "temperatureC typed as number (not number|null) — null/NaN handling deferred to CityMarker render site"
  - "Relative URL /api/cities/temperatures used exclusively — Vite proxy forwards to backend, eliminating CORS dependency"
  - "retryCount in useEffect dependency array is the standard React retry pattern — increment triggers re-fetch"
metrics:
  duration: "~10 minutes"
  completed: "2026-04-29"
  tasks_completed: 2
  tasks_total: 2
---

# Phase 2 Plan 02: CityTemperature Type and useCityTemperatures Hook Summary

**One-liner:** TypeScript data contract (CityTemperature interface) and fetch hook (useCityTemperatures) with cancelled-flag cancel-on-unmount pattern, retryCount-triggered re-fetch, and Array.isArray runtime validation.

## What Was Built

### Task 1 — `src/weather-ui/src/types/cityTemperature.ts`

Created the TypeScript interface matching the backend DTO (`CityTemperature.cs`) exactly:

```typescript
export interface CityTemperature {
  nameNative:   string;   // Bulgarian city name e.g. "София"
  nameEn:       string;   // English city name e.g. "Sofia"
  latitude:     number;
  longitude:    number;
  temperatureC: number;   // backend returns double? — handle null/NaN at render time, not here
}
```

- Named export only, no default export (consistent with deleted `types/weather.ts` pattern)
- No barrel `index.ts` — consumers import via direct path `'../types/cityTemperature'`
- `temperatureC` is `number` not `number | null` — null/NaN handled at render via `Math.round(city.temperatureC ?? NaN)` and `isNaN()` guard in CityMarker

### Task 2 — `src/weather-ui/src/hooks/useCityTemperatures.ts`

Created the data-fetching hook implementing:

- **Relative URL** `/api/cities/temperatures` — Vite proxy routes to `http://localhost:5055/api/cities/temperatures`, eliminating CORS requirement (PITFALLS §4/§8)
- **Cancelled-flag pattern** — `let cancelled = false` in effect, cleanup sets `cancelled = true`; every state setter guarded with `if (!cancelled)` (preserves pattern from deleted `useWeatherData.ts`)
- **retryCount trigger** — `retryCount` state in the `useEffect` dependency array; `retry()` increments it to re-run the effect; returned for use by the Error Overlay's "Try again" button (D-12)
- **Array.isArray() runtime validation** — validates response shape before cast (T-02-02 threat mitigation)
- **Error message** — raw `err.message` stored in state; `App.tsx` (Plan 04) displays a fixed user-facing string — raw message never rendered in UI (T-02-01 threat mitigation)
- Returns `{ data: CityTemperature[] | null, loading: boolean, error: string | null, retry: () => void }`

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 — CityTemperature interface | `0f2a479` | feat(02-02): add CityTemperature TypeScript interface matching backend DTO |
| 2 — useCityTemperatures hook  | `ae3ccc6` | feat(02-02): add useCityTemperatures hook with cancelled-flag pattern and retry |

## Deviations from Plan

None — plan executed exactly as written.

## Threat Model Compliance

| Threat ID | Disposition | Status |
|-----------|-------------|--------|
| T-02-01 | mitigate | ✅ Raw error message stored in hook state only; App.tsx (Plan 04) renders fixed string |
| T-02-02 | mitigate | ✅ `Array.isArray(json)` check before cast — non-array response sets error state cleanly |

## Known Stubs

None — these files define pure types and a pure fetch hook with no rendering. No stub patterns present.

## Threat Flags

None — no new network endpoints, auth paths, file access patterns, or schema changes introduced beyond what the plan specified.

## Self-Check: PASSED

- [x] `src/weather-ui/src/types/cityTemperature.ts` exists
- [x] `src/weather-ui/src/hooks/useCityTemperatures.ts` exists
- [x] Commit `0f2a479` exists (Task 1)
- [x] Commit `ae3ccc6` exists (Task 2)
- [x] All acceptance criteria verified via automated checks
- [x] Plan 02-02 verification script passed
