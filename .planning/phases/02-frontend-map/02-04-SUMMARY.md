---
phase: 02-frontend-map
plan: 04
subsystem: ui
tags: [react, tailwind, typescript, loading-overlay, error-overlay, app-shell]

# Dependency graph
requires:
  - phase: 02-02
    provides: useCityTemperatures hook (data, loading, error, retry)
  - phase: 02-03
    provides: BulgariaMap component (accepts cities prop)
provides:
  - App.tsx: full-viewport shell wiring hook → map → loading/error overlays
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Relative wrapper div as overlay stacking context (D-03)"
    - "Always-rendered BulgariaMap so tiles load immediately beneath overlays (D-11)"
    - "Dual conditional overlays (loading / error) at z-[1000] — above Leaflet controls at z-800 (D-13)"

key-files:
  created: []
  modified:
    - src/weather-ui/src/App.tsx

key-decisions:
  - "BulgariaMap renders unconditionally — tiles start loading even during spinner phase (D-11)"
  - "Error overlay is shown regardless of loading state — no !loading guard needed since useCityTemperatures sets error only after loading=false"
  - "z-[1000] on both overlays is mandatory (D-13) — Leaflet controls sit at z-800"
  - "No App.css import — deleted in Wave 1 per D-04; all styles via Tailwind v4 utilities"
  - "font-bold only on h2 and button — font-semibold intentionally avoided (UI-SPEC 2-weight constraint)"

patterns-established:
  - "Full-viewport shell pattern: relative w-screen h-screen overflow-hidden wrapper"
  - "Overlay pattern: absolute inset-0 z-[1000] bg-black/45 flex items-center justify-center"

requirements-completed: [UI-02, UI-03, UI-04]

# Metrics
duration: ~10min
completed: 2026-04-29
---

# Phase 2 Plan 04: App Assembly Summary

**App.tsx rewritten as a thin orchestration shell — data hook wired to full-viewport map with loading spinner and error card overlays.**

## Performance

- **Duration:** ~10 min
- **Completed:** 2026-04-29
- **Tasks:** 2 (code rewrite + human visual checkpoint)
- **Files modified:** 1

## Accomplishments

- `App.tsx` fully rewritten — all legacy code removed (`useWeatherData`, `WeatherMap`, header/footer chrome, `App.css` import)
- Hook wired: `const { data, loading, error, retry } = useCityTemperatures()`
- `<BulgariaMap cities={data} />` renders unconditionally so tiles begin loading immediately
- Loading overlay: `absolute inset-0 z-[1000] bg-black/45` with white animated spinner (`role="status"`, `aria-label="Loading temperature data…"`)
- Error overlay: same backdrop, white card with `border-l-4 border-red-500`, ⚠️ icon, exact UI-SPEC copy, and `<button onClick={retry}>Try again</button>`
- `npm run build` — zero TypeScript errors
- Human visual checkpoint passed — all 5 interactive behaviours confirmed by user

## Task Commits

1. **Task 1: Rewrite App.tsx** — `a4656b4` (feat)
2. **Task 2: Visual checkpoint** — passed by user on 2026-04-29

## Files Created/Modified

- `src/weather-ui/src/App.tsx` — Complete rewrite: full-viewport shell, `useCityTemperatures` hook, `BulgariaMap`, loading spinner overlay, error card overlay with retry

## Decisions Made

- `BulgariaMap` is always rendered (not conditionally on data): map tiles start loading immediately and are visible beneath the loading overlay — better perceived performance
- No `!loading` guard on error overlay: `useCityTemperatures` only sets `error` after `loading` becomes false, so the two states are mutually exclusive
- `z-[1000]` on both overlays is a hard requirement (D-13): Leaflet's zoom controls and attribution render at z-800; overlays must be above them
- Font weights: `font-bold` only — `font-semibold` intentionally avoided per UI-SPEC 2-weight constraint (400 + 700 only)

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## Human Checkpoint Result

✅ **APPROVED** by user on 2026-04-29

All 5 verification points confirmed:
1. Loading spinner overlay renders before data arrives
2. Full-viewport map, no chrome, CartoDB Positron tiles
3. 28 colour-coded temperature badges
4. Hover tooltips showing English city names
5. Error card with "Try again" retry flow

## Next Phase Readiness

Phase 2 is complete. All 5 ROADMAP success criteria verified (VERIFICATION.md).

---
*Phase: 02-frontend-map*
*Completed: 2026-04-29*

## Self-Check: PASSED

- `src/weather-ui/src/App.tsx` — FOUND ✓
- Commit `a4656b4` — FOUND ✓
- Human checkpoint — APPROVED ✓
