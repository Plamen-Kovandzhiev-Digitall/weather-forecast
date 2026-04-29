---
phase: 02-frontend-map
reviewed: 2025-01-27T00:00:00Z
depth: standard
files_reviewed: 8
files_reviewed_list:
  - src/weather-ui/package.json
  - src/weather-ui/vite.config.ts
  - src/weather-ui/src/index.css
  - src/weather-ui/src/types/cityTemperature.ts
  - src/weather-ui/src/hooks/useCityTemperatures.ts
  - src/weather-ui/src/components/CityMarker.tsx
  - src/weather-ui/src/components/BulgariaMap.tsx
  - src/weather-ui/src/App.tsx
findings:
  critical: 1
  warning: 5
  info: 3
  total: 9
status: issues_found
---

# Phase 02: Code Review Report — Bulgaria Weather Map Frontend

**Reviewed:** 2025-01-27  
**Depth:** standard  
**Files Reviewed:** 8  
**Status:** issues_found

---

## Summary

Eight files were reviewed: the React 19 + TypeScript SPA built with Vite, Tailwind v4, and react-leaflet v5. The overall architecture is sound — the hook/component split is clean, the cancellation flag pattern is present, DivIcon memoization is correct, and the Vite proxy eliminates CORS dependency. However, the app has **no React Error Boundary**, which combined with unvalidated API response objects creates a full silent-crash path. There are also five warnings spanning NaN colour logic, fetch abort hygiene, invalid ARIA usage, type–runtime divergence, and an XSS-prone template pattern in Leaflet icon construction.

---

## Critical Issues

### CR-01: No Error Boundary + Unvalidated API Elements → Silent White-Screen Crash

**File:** `src/weather-ui/src/hooks/useCityTemperatures.ts:29-32` and `src/weather-ui/src/App.tsx`

**Issue:**  
`useCityTemperatures` validates that the fetch response is an array but immediately casts it to `CityTemperature[]` without inspecting individual elements:

```ts
if (!Array.isArray(json)) {
  throw new Error('Unexpected response format from weather API');
}
if (!cancelled) setData(json as CityTemperature[]);  // ← blind cast
```

If the backend sends any element where `latitude` or `longitude` is `null`, `undefined`, or a non-numeric string, Leaflet's `<Marker position={[city.latitude, city.longitude]}>` will throw a JavaScript error during React's render cycle. Because there is **no `<ErrorBoundary>` anywhere in the component tree**, this exception propagates all the way up, unmounts the entire app, and leaves the user with a blank white screen — no error message, no retry button. The only recovery is a hard page refresh.

**Fix:**

Step 1 — add element validation in the hook to fail fast with a useful error:
```ts
function isCityTemperature(v: unknown): v is CityTemperature {
  if (typeof v !== 'object' || v === null) return false;
  const o = v as Record<string, unknown>;
  return (
    typeof o.nameNative === 'string' &&
    typeof o.nameEn     === 'string' &&
    typeof o.latitude   === 'number' &&
    typeof o.longitude  === 'number'
    // temperatureC may be null per D-08 — intentionally not required here
  );
}

// inside fetchData(), replace the cast:
if (!json.every(isCityTemperature)) {
  throw new Error('Malformed city data received from weather API');
}
if (!cancelled) setData(json as CityTemperature[]);
```

Step 2 — wrap `<BulgariaMap>` (or the App root) in an Error Boundary so any remaining render-time failure shows a recoverable UI rather than a blank screen:
```tsx
// src/components/MapErrorBoundary.tsx
import { Component, type ReactNode } from 'react';

interface State { hasError: boolean }
export class MapErrorBoundary extends Component<{ children: ReactNode }, State> {
  state: State = { hasError: false };
  static getDerivedStateFromError() { return { hasError: true }; }
  render() {
    if (this.state.hasError)
      return <div className="...">Map failed to render. Please refresh.</div>;
    return this.props.children;
  }
}

// In App.tsx:
<MapErrorBoundary>
  <BulgariaMap cities={data} />
</MapErrorBoundary>
```

---

## Warnings

### WR-01: `getTempColorClass(NaN)` Returns `bg-red-500` for Missing Data

**File:** `src/weather-ui/src/components/CityMarker.tsx:8-13` and `28`

**Issue:**  
When `city.temperatureC` is `null`/`undefined` at runtime, the `?? NaN` guard produces `NaN`, and `Math.round(NaN)` stays `NaN`. `isNaN(NaN)` correctly renders the label as `—`. However `getTempColorClass` receives `NaN`:

```ts
if (temp <= 0)  return 'bg-blue-500';   // NaN <= 0  → false
if (temp <= 15) return 'bg-teal-500';   // NaN <= 15 → false
if (temp <= 25) return 'bg-green-500';  // NaN <= 25 → false
if (temp <= 35) return 'bg-orange-500'; // NaN <= 35 → false
return 'bg-red-500';                    // ← NaN falls through to hottest colour
```

A city with no temperature data is displayed as a **red badge labelled `—`**, indistinguishable at a glance from a city reporting `>35 °C`. Users will misread missing data as a dangerously hot city.

**Fix:**
```ts
function getTempColorClass(temp: number): string {
  if (isNaN(temp)) return 'bg-gray-400';  // explicit no-data colour
  if (temp <= 0)   return 'bg-blue-500';
  if (temp <= 15)  return 'bg-teal-500';
  if (temp <= 25)  return 'bg-green-500';
  if (temp <= 35)  return 'bg-orange-500';
  return 'bg-red-500';
}
```

---

### WR-02: Fetch Not Aborted — `AbortController` Missing

**File:** `src/weather-ui/src/hooks/useCityTemperatures.ts:17-43`

**Issue:**  
The `cancelled` flag prevents stale state from being written after unmount, but it does **not** cancel the in-flight HTTP request. The browser continues to receive, decompress, and parse the response even though the result will be discarded. More critically, every call to `retry()` increments `retryCount`, triggering the effect and launching a **new concurrent fetch** while the previous one is still in flight. Under rapid retries (e.g., a flaky network), multiple concurrent requests pile up, all returning and all being silently ignored.

**Fix:**
```ts
useEffect(() => {
  const controller = new AbortController();

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch('/api/cities/temperatures', {
        signal: controller.signal,          // ← abort on cleanup
      });
      if (!response.ok) {
        throw new Error(`API error: ${response.status} ${response.statusText}`);
      }
      const json: unknown = await response.json();
      if (!Array.isArray(json)) {
        throw new Error('Unexpected response format from weather API');
      }
      setData(json as CityTemperature[]);
    } catch (err) {
      if (err instanceof DOMException && err.name === 'AbortError') return; // ignore abort
      setError(err instanceof Error ? err.message : 'Failed to fetch temperature data');
    } finally {
      setLoading(false);
    }
  };

  fetchData();
  return () => controller.abort();          // ← cancels in-flight request on cleanup
}, [retryCount]);
```

---

### WR-03: `aria-label` on a Bare `<div>` Is Invisible to Screen Readers

**File:** `src/weather-ui/src/components/BulgariaMap.tsx:29-33`

**Issue:**  
```jsx
<div
  className="h-screen w-screen"
  aria-label="Bulgaria weather map showing current temperatures for 28 district cities"
>
```
A generic `<div>` has the implicit ARIA role `none`/`presentation`. Per ARIA 1.1, `aria-label` is only honoured on elements with a semantic role that permits naming. Screen readers will silently ignore this label — it provides zero accessibility benefit as written.

**Fix:** Add an explicit landmark role so the label is announced:
```jsx
<div
  role="region"
  aria-label="Bulgaria weather map showing current temperatures for 28 district cities"
  className="h-screen w-screen"
>
```

---

### WR-04: `CityTemperature.temperatureC` Typed `number` but Defended Against `null`

**File:** `src/weather-ui/src/types/cityTemperature.ts:6` and `src/weather-ui/src/components/CityMarker.tsx:28`

**Issue:**  
The interface declares `temperatureC: number` (non-nullable), but the same file's comment says *"backend returns double? — handle null/NaN at render time"*, and `CityMarker.tsx` explicitly guards with `city.temperatureC ?? NaN`. The TypeScript type is lying about the runtime contract. This means:

- `isCityTemperature` element guard written per CR-01 fix will reject `null` temperatureC values at validation time (before the `?? NaN` fallback can fire), potentially discarding valid city records that simply lack a current reading.
- Any developer reading the type sees `number` and reasonably omits the null guard in future code.

**Fix:** Align the type with reality:
```ts
export interface CityTemperature {
  nameNative:   string;
  nameEn:       string;
  latitude:     number;
  longitude:    number;
  temperatureC: number | null;   // backend double? can be null when no reading available
}
```
Update `isCityTemperature` to permit `null` for this field, and ensure `CityMarker.tsx` continues handling `null` (the existing `?? NaN` already does).

---

### WR-05: L.divIcon HTML Built with Unescaped Template Literal — Latent XSS Pattern

**File:** `src/weather-ui/src/components/CityMarker.tsx:19-25`

**Issue:**  
```ts
html: `<div class="${colorClass} ...">${label}</div>`,
```
Both `colorClass` (hardcoded string from a pure function) and `label` (derived from `Math.round(number)`) are safe today. However, this pattern inserts arbitrary strings directly into a raw HTML string that Leaflet injects into the DOM via `innerHTML`. There is **no TypeScript warning, no ESLint rule, and no code comment** that would warn a future developer not to add `city.nameEn` or another API-origin string to this template. City names from the backend (e.g., `<img src=x onerror=alert(1)>`) would execute immediately.

**Fix (preferred):** Eliminate the string template by building the icon element with DOM APIs:
```ts
function createTempIcon(temp: number): L.DivIcon {
  const colorClass = getTempColorClass(temp);
  const label = isNaN(temp) ? '—' : `${temp}°C`;

  // Build DOM node so no string-to-HTML path exists
  const el = document.createElement('div');
  el.className = `${colorClass} text-white text-[13px] font-bold px-2 py-1 rounded shadow-md whitespace-nowrap leading-none select-none`;
  el.textContent = label;          // textContent is XSS-safe by definition

  return L.divIcon({
    className: '',
    html: el,                      // L.divIcon accepts HTMLElement directly
    iconSize:   [52, 28],
    iconAnchor: [26, 14],
  });
}
```

---

## Info

### IN-01: Hardcoded Infrastructure URL in User-Facing Error Message

**File:** `src/weather-ui/src/App.tsx:33-35`

**Issue:**  
```jsx
<p>The weather service is unavailable. Make sure the backend is running at localhost:5055, then try again.</p>
```
This bakes a hard-coded port (`5055`) into the production bundle. It is meaningless to end users on a deployed environment and leaks the internal service topology to anyone who opens DevTools.

**Fix:** Replace with a generic message:
```jsx
<p className="text-sm text-gray-600 leading-relaxed">
  The weather service is temporarily unavailable. Please try again in a moment.
</p>
```
If the dev guidance is needed, keep it in a `README` or behind `import.meta.env.DEV`.

---

### IN-02: No Test Script in `package.json`

**File:** `src/weather-ui/package.json`

**Issue:** There is no `test` script. The colour-classification logic (`getTempColorClass`), the NaN guard, and the array validation in `useCityTemperatures` are all pure or near-pure functions that are trivial to unit-test. Zero test coverage means the NaN-colour bug described in WR-01 and regressions to the retry logic would never be caught automatically.

**Fix:** Add Vitest (natural fit for Vite projects):
```json
"scripts": {
  "test": "vitest run",
  "test:watch": "vitest"
},
"devDependencies": {
  "vitest": "^2.0.0",
  "@testing-library/react": "^16.0.0"
}
```

---

### IN-03: Module-Level Side Effect Mutates Leaflet Global Prototype

**File:** `src/weather-ui/src/components/BulgariaMap.tsx:13-18`

**Issue:**
```ts
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({ iconUrl: markerIcon, ... });
```
These statements run at **module evaluation time**, mutating Leaflet's global `Icon.Default` prototype. In Vite HMR, module re-evaluation runs on every hot-reload, so this mutation fires repeatedly. While harmless in practice (it is idempotent by coincidence), it is a code smell: side effects at module scope are difficult to test in isolation and can produce surprising behaviour if `BulgariaMap` is ever imported in a test environment or server-side context.

**Fix:** Move the patch inside a `useEffect` with an empty dependency array, or into the `main.tsx` entry point (run-once guarantee):
```ts
// In main.tsx — runs exactly once, not on every HMR reload
import 'leaflet/dist/leaflet.css';
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
// ... patch L.Icon.Default here
```

---

## Findings Index

| ID    | Severity | File                                   | Summary                                           |
|-------|----------|----------------------------------------|---------------------------------------------------|
| CR-01 | BLOCKER  | `useCityTemperatures.ts` + `App.tsx`   | No ErrorBoundary + unvalidated array elements → silent white-screen crash |
| WR-01 | WARNING  | `CityMarker.tsx:8-13,28`               | `getTempColorClass(NaN)` returns red (hottest) for missing data |
| WR-02 | WARNING  | `useCityTemperatures.ts:17-43`         | Fetch not aborted via AbortController; concurrent retries pile up |
| WR-03 | WARNING  | `BulgariaMap.tsx:29-33`                | `aria-label` on bare `<div>` — no role, invisible to screen readers |
| WR-04 | WARNING  | `cityTemperature.ts:6` + `CityMarker.tsx:28` | `temperatureC: number` typed non-nullable but defended as nullable |
| WR-05 | WARNING  | `CityMarker.tsx:19-25`                 | L.divIcon html built via template literal — latent XSS if extended |
| IN-01 | INFO     | `App.tsx:33-35`                        | Hardcoded `localhost:5055` in production error message |
| IN-02 | INFO     | `package.json`                         | No test script or test coverage |
| IN-03 | INFO     | `BulgariaMap.tsx:13-18`                | Module-level Leaflet prototype mutation — not idempotent under HMR |

---

_Reviewed: 2025-01-27_  
_Reviewer: gsd-code-reviewer (adversarial, standard depth)_  
_Depth: standard_
