---
phase: 02-frontend-map
fixed_at: 2025-01-27T00:00:00Z
review_path: .planning/phases/02-frontend-map/02-REVIEW.md
iteration: 1
findings_in_scope: 6
fixed: 6
skipped: 0
status: all_fixed
---

# Phase 02: Code Review Fix Report — Bulgaria Weather Map Frontend

**Fixed at:** 2025-01-27  
**Source review:** `.planning/phases/02-frontend-map/02-REVIEW.md`  
**Iteration:** 1

**Summary:**
- Findings in scope: 6 (CR-01, WR-01, WR-02, WR-03, WR-04, WR-05)
- Fixed: 6
- Skipped: 0

---

## Fixed Issues

### CR-01: No Error Boundary + Unvalidated API Elements → Silent White-Screen Crash

**Files modified:** `src/weather-ui/src/hooks/useCityTemperatures.ts`, `src/weather-ui/src/components/MapErrorBoundary.tsx`, `src/weather-ui/src/App.tsx`  
**Commit:** `7237eb1`  
**Applied fix:**
- Added `isCityTemperature(item: unknown): item is CityTemperature` type guard that validates all 5 fields (nameNative, nameEn, latitude, longitude, and temperatureC as `number | null`).
- Replaced blind `json as CityTemperature[]` cast with `json.filter(isCityTemperature)` so malformed elements are silently dropped rather than crashing Leaflet.
- Created `src/weather-ui/src/components/MapErrorBoundary.tsx` — a React class `ErrorBoundary` that catches render-time exceptions from `<BulgariaMap>` and shows a recoverable error card UI (dark overlay, white card, "Unable to load weather data", "The map failed to load. Please refresh the page.", a "Refresh" button).
- Wrapped `<BulgariaMap cities={data} />` in `<MapErrorBoundary>` in `App.tsx`.

> **Note:** WR-02 (AbortController) was co-applied in this commit because it required rewriting the same `useEffect` block in `useCityTemperatures.ts`. See WR-02 entry below for full details.

---

### WR-01: `getTempColorClass(NaN)` Returns `bg-red-500` for Missing Data

**Files modified:** `src/weather-ui/src/components/CityMarker.tsx`  
**Commit:** `45c2881`  
**Applied fix:** Added `if (isNaN(temp)) return 'bg-gray-400'` as the first guard in `getTempColorClass`. Cities with no temperature reading now display a neutral gray badge instead of the hottest red colour.

---

### WR-02: Fetch Not Aborted — `AbortController` Missing

**Files modified:** `src/weather-ui/src/hooks/useCityTemperatures.ts`  
**Commit:** `7237eb1` *(co-applied with CR-01 — same file, same useEffect rewrite)*  
**Applied fix:**
- Replaced the `cancelled` boolean flag with `const controller = new AbortController()` inside the `useEffect`.
- Passed `{ signal: controller.signal }` to the `fetch` call so in-flight HTTP requests are actually cancelled on cleanup.
- Cleanup function now calls `controller.abort()` instead of setting a flag.
- Added `if (err instanceof DOMException && err.name === 'AbortError') return` to ignore intentional aborts without setting error state.
- The `finally` block guards on `controller.signal.aborted` to skip `setLoading(false)` after intentional abort.

---

### WR-03: `aria-label` on a Bare `<div>` Is Invisible to Screen Readers

**Files modified:** `src/weather-ui/src/components/BulgariaMap.tsx`  
**Commit:** `a11d7a4`  
**Applied fix:** Added `role="region"` to the wrapper `<div>` that carries the `aria-label`. Screen readers now recognise the label and announce the map region.

---

### WR-04: `CityTemperature.temperatureC` Typed `number` but Defended Against `null`

**Files modified:** `src/weather-ui/src/types/cityTemperature.ts`  
**Commit:** `8a0c110`  
**Applied fix:** Changed `temperatureC: number` to `temperatureC: number | null` in the `CityTemperature` interface. The existing `city.temperatureC ?? NaN` guard in `CityMarker.tsx` continues to work correctly with the updated type (TypeScript confirmed zero errors via `npm run build`). The `isCityTemperature` guard in the hook (applied in CR-01) already accepts `null` for this field.

---

### WR-05: L.divIcon HTML Built with Unescaped Template Literal — Latent XSS Pattern

**Files modified:** `src/weather-ui/src/components/CityMarker.tsx`  
**Commit:** `63a40be`  
**Applied fix:** Replaced the template literal `html: \`<div class="${colorClass}">${label}</div>\`` with DOM element construction using `document.createElement('div')`, setting `el.className` and `el.textContent = label`. Passes the `HTMLElement` directly to `L.divIcon({ html: el })` (supported by Leaflet). `textContent` is XSS-safe by definition — future developers cannot accidentally inject HTML by adding city name fields to the label.

---

## Skipped Issues

None — all in-scope findings were fixed.

---

## Build Verification

`cd src/weather-ui && npm run build` completed successfully after all fixes:

```
✓ 81 modules transformed.
dist/index.html              0.79 kB │ gzip:   0.43 kB
dist/assets/index-*.css     29.12 kB │ gzip:   9.85 kB
dist/assets/index-*.js     360.36 kB │ gzip: 113.22 kB
✓ built in 2.65s
```

Zero TypeScript errors.

---

_Fixed: 2025-01-27_  
_Fixer: gsd-code-fixer_  
_Iteration: 1_
