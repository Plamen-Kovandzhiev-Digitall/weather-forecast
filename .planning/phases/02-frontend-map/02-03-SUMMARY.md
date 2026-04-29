---
phase: 02-frontend-map
plan: 03
subsystem: ui
tags: [react, leaflet, react-leaflet, tailwind, typescript, divicon, cartocdn]

# Dependency graph
requires:
  - phase: 02-02
    provides: CityTemperature interface used by both CityMarker and BulgariaMap props
provides:
  - CityMarker component: memoized L.divIcon temperature badge with color coding and tooltip
  - BulgariaMap component: full-viewport MapContainer with CartoDB Positron tiles and 28 CityMarker instances
affects:
  - 02-04-PLAN.md (App assembly imports BulgariaMap and passes cities prop)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "L.divIcon with className:'' to clear Leaflet default white box (D-07)"
    - "useMemo([roundedTemp]) per CityMarker to prevent 28-marker flicker (PITFALL §6)"
    - "Immutable MapContainer constants for center/zoom (PITFALL §7)"
    - "Leaflet default icon fix: delete _getIconUrl + mergeOptions with Vite-resolved asset URLs"

key-files:
  created:
    - src/weather-ui/src/components/CityMarker.tsx
    - src/weather-ui/src/components/BulgariaMap.tsx
  modified: []

key-decisions:
  - "className:'' on L.divIcon is non-negotiable — without it Leaflet's leaflet-div-icon CSS overrides all Tailwind badge styling (D-07)"
  - "useMemo dependency array is [roundedTemp] not [city.temperatureC] — avoids unnecessary recreations for sub-integer fluctuations (D-08)"
  - "BULGARIA_CENTER and zoom=7 are module-level constants, never state — MapContainer props immutable after mount (PITFALL §7)"
  - "Tooltip children is city.nameEn (English name) per D-10; city.nameNative excluded to avoid Bulgarian character reconciliation edge cases"
  - "Leaflet default icon fix included in BulgariaMap.tsx even though only DivIcon markers are used — prevents broken image errors for any future fallback Marker"

patterns-established:
  - "CityMarker pattern: per-marker useMemo keyed to rounded temperature value"
  - "DivIcon pattern: className empty string + Tailwind classes in html string"
  - "Null/NaN guard pattern: city.temperatureC ?? NaN → Math.round(NaN) → isNaN → render '—'"

requirements-completed: [MAP-01, MAP-02, MAP-03, MAP-04, UI-04]

# Metrics
duration: 15min
completed: 2026-04-29
---

# Phase 2 Plan 03: Map Components Summary

**Memoized L.divIcon temperature badges (CityMarker) and full-viewport CartoDB Positron map (BulgariaMap) with color-coded markers for 28 Bulgarian district cities**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-04-29T00:00:00Z
- **Completed:** 2026-04-29T00:15:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- `CityMarker.tsx`: memoized L.divIcon badge with 5-range color coding (blue/teal/green/orange/red), isNaN guard rendering `—`, and hover Tooltip showing English city name
- `BulgariaMap.tsx`: full-viewport MapContainer centered on [42.7, 25.5] zoom 7 with CartoDB Positron tiles, Leaflet default icon fix, and `cities?.map` rendering one CityMarker per city
- All three critical mandates enforced: `className:''` (D-07), `useMemo([roundedTemp])` (PITFALL §6), immutable center/zoom constants (PITFALL §7)

## Task Commits

Each task was committed atomically:

1. **Task 1: CityMarker.tsx — memoized DivIcon badge** - `3c919c8` (feat)
2. **Task 2: BulgariaMap.tsx — CartoDB Positron map** - `6951605` (feat)

## Files Created/Modified

- `src/weather-ui/src/components/CityMarker.tsx` — Memoized L.divIcon temperature badge: getTempColorClass (5 ranges), createTempIcon (generates Tailwind-styled HTML), CityMarker component with useMemo([roundedTemp]) and Tooltip
- `src/weather-ui/src/components/BulgariaMap.tsx` — Full-viewport MapContainer: CartoDB Positron TileLayer, Leaflet default icon fix, cities?.map with CityMarker, h-screen/h-full viewport sizing

## Decisions Made

- `className:''` on `L.divIcon` is a hard requirement (D-07): Leaflet's default `leaflet-div-icon` CSS rule adds `background: white; border: 1px solid #666` which overrides Tailwind color classes if className is not explicitly cleared
- `useMemo` dependency is `[roundedTemp]` not `[city.temperatureC]` (D-08): rounding before memoizing means sub-integer temperature changes (e.g., 14.1 → 14.7°C) don't trigger unnecessary icon recreations
- `BULGARIA_CENTER` and `zoom={7}` are module-level constants (PITFALL §7): react-leaflet's MapContainer silently ignores prop changes after mount
- Tooltip children is `{city.nameEn}` per D-10: English names are the display identifier; Bulgarian names are stored as `nameNative` but not shown in the tooltip
- Leaflet default icon fix (`delete _getIconUrl` + `mergeOptions`) is included even though all markers use DivIcon: prevents broken icon image errors if any future fallback Marker is introduced

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- `CityMarker` and `BulgariaMap` are ready for import in `App.tsx` (Plan 04)
- `BulgariaMap` accepts `cities: CityTemperature[] | null` — null renders tiles with no markers (loading state from App.tsx overlay)
- No blockers for Plan 04

---
*Phase: 02-frontend-map*
*Completed: 2026-04-29*

## Self-Check: PASSED

- `src/weather-ui/src/components/CityMarker.tsx` — FOUND ✓
- `src/weather-ui/src/components/BulgariaMap.tsx` — FOUND ✓
- Commit `3c919c8` — FOUND ✓
- Commit `6951605` — FOUND ✓
