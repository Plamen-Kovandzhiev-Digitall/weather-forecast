# Phase 2: Frontend Map — Pattern Map

**Mapped:** 2026-04-28
**Files analyzed:** 11 (4 new + 7 rewritten/modified)
**Analogs found:** 11 / 11

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `src/types/cityTemperature.ts` | type | static | `src/types/weather.ts` | exact |
| `src/hooks/useCityTemperatures.ts` | hook | request-response | `src/hooks/useWeatherData.ts` | exact |
| `src/components/BulgariaMap.tsx` | component | request-response | `src/components/WeatherMap.tsx` | exact |
| `src/components/CityMarker.tsx` | component | transform | `src/components/WeatherMap.tsx` + `WeatherPopup.tsx` | role-match |
| `src/App.tsx` _(rewrite)_ | component | request-response | `src/App.tsx` | exact |
| `src/types/weather.ts` _(delete)_ | type | static | — | replaced |
| `src/components/WeatherMap.tsx` _(delete)_ | component | — | — | replaced |
| `src/hooks/useWeatherData.ts` _(delete)_ | hook | — | — | replaced |
| `src/index.css` _(modify)_ | style/config | static | `src/index.css` | exact |
| `vite.config.ts` _(modify)_ | config | static | `vite.config.ts` | exact |
| `package.json` _(modify)_ | config | static | `package.json` | exact |

> All paths are relative to `src/weather-ui/src/` unless the file is at the `src/weather-ui/` root.

---

## Pattern Assignments

---

### `src/types/cityTemperature.ts` (type, static)

**Analog:** `src/weather-ui/src/types/weather.ts`

**Core pattern** (lines 1–6 of analog — entire file):
```typescript
// ANALOG: src/weather-ui/src/types/weather.ts  lines 1-6
export interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}
```

**New file must follow exactly the same shape — one named export, no default export:**
```typescript
// NEW: src/weather-ui/src/types/cityTemperature.ts
export interface CityTemperature {
  nameNative:   string;   // Bulgarian name e.g. "София"
  nameEn:       string;   // English name e.g. "Sofia"
  latitude:     number;
  longitude:    number;
  temperatureC: number;   // double? from backend — handle null/NaN gracefully
}
```

**Notes:**
- `temperatureC` typed as `number` (TypeScript). Handle `null | undefined | NaN` at render time, not in the type.
- No barrel `index.ts` in the project — import directly: `import type { CityTemperature } from '../types/cityTemperature'`.

---

### `src/hooks/useCityTemperatures.ts` (hook, request-response)

**Analog:** `src/weather-ui/src/hooks/useWeatherData.ts` — **entire file is the template**

**Imports pattern** (analog lines 1–2):
```typescript
// ANALOG: src/weather-ui/src/hooks/useWeatherData.ts  lines 1-2
import { useState, useEffect } from 'react';
import type { WeatherForecast } from '../types/weather';
```
→ Replace `WeatherForecast` with `CityTemperature` from `'../types/cityTemperature'`.

**Return-type interface** (analog lines 4–8):
```typescript
// ANALOG: src/weather-ui/src/hooks/useWeatherData.ts  lines 4-8
interface UseWeatherDataResult {
  data: WeatherForecast[] | null;
  loading: boolean;
  error: string | null;
}
```
→ Rename interface to `UseCityTemperaturesResult`; change `data` type to `CityTemperature[] | null`.
→ **Add `retry: () => void`** to the return type — required by the Error Overlay's "Try again" button (D-12, UI-SPEC interaction contract).

**State declarations** (analog lines 11–13):
```typescript
// ANALOG: src/weather-ui/src/hooks/useWeatherData.ts  lines 11-13
const [data, setData] = useState<WeatherForecast[] | null>(null);
const [loading, setLoading] = useState(true);
const [error, setError] = useState<string | null>(null);
```
→ Change generic type; otherwise copy verbatim.

**Cancel-on-unmount pattern** (analog lines 15–39) — **preserve this exactly**:
```typescript
// ANALOG: src/weather-ui/src/hooks/useWeatherData.ts  lines 15-39
useEffect(() => {
  let cancelled = false;

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch('http://localhost:5055/weatherforecast');
      if (!response.ok) {
        throw new Error(`API error: ${response.status} ${response.statusText}`);
      }
      const json: WeatherForecast[] = await response.json();
      if (!cancelled) setData(json);
    } catch (err) {
      if (!cancelled) {
        setError(err instanceof Error ? err.message : 'Failed to fetch forecast data');
      }
    } finally {
      if (!cancelled) setLoading(false);
    }
  };

  fetchData();
  return () => { cancelled = true; };
}, []);
```

**Diff from analog for new file:**
1. URL: `'http://localhost:5055/weatherforecast'` → `'http://localhost:5055/api/cities/temperatures'`
2. JSON cast: `WeatherForecast[]` → `CityTemperature[]`
3. Error fallback message: `'Failed to fetch forecast data'` → `'Failed to fetch temperature data'`
4. `useEffect` dependency array: add a `retryCount` state variable so re-fetch is triggered:
   ```typescript
   const [retryCount, setRetryCount] = useState(0);
   // ...
   useEffect(() => { /* fetchData */ }, [retryCount]);
   const retry = () => setRetryCount(c => c + 1);
   return { data, loading, error, retry };
   ```
5. Return: add `retry` to the return object.

**Pitfall guard (PITFALLS.md §11):** The old URL `http://localhost:5055/weatherforecast` must not appear anywhere in the new file.

---

### `src/components/BulgariaMap.tsx` (component, request-response)

**Analog:** `src/weather-ui/src/components/WeatherMap.tsx` — **entire file is the structural template**

**Imports pattern** (analog lines 1–17):
```typescript
// ANALOG: src/weather-ui/src/components/WeatherMap.tsx  lines 1-17
import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { WeatherForecast } from '../types/weather';
import { WeatherPopup } from './WeatherPopup';

// Fix Leaflet default marker icons for bundlers
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
});
```

**Changes for new file:**
- Replace `Marker, Popup` import with `{ MapContainer, TileLayer }` (markers handled by `CityMarker` sub-component).
- Drop `WeatherPopup` import; add `import { CityMarker } from './CityMarker'`.
- Drop `import type { WeatherForecast }` → `import type { CityTemperature } from '../types/cityTemperature'`.
- **Keep the Leaflet default icon fix** (lines 12–17) — harmless with DivIcon-only markers, avoids broken image URLs if a fallback Marker is ever introduced.

**Props interface** (analog lines 21–25):
```typescript
// ANALOG: src/weather-ui/src/components/WeatherMap.tsx  lines 21-25
interface WeatherMapProps {
  forecasts: WeatherForecast[] | null;
  loading: boolean;
  error: string | null;
}
```
→ New props: `cities: CityTemperature[] | null` (loading/error state moves to `App.tsx` overlays; `BulgariaMap` only receives ready data).

**MapContainer core pattern** (analog lines 30–36):
```typescript
// ANALOG: src/weather-ui/src/components/WeatherMap.tsx  lines 30-36
<MapContainer
  center={VT_COORDS}
  zoom={12}
  scrollWheelZoom
  className="leaflet-map"
  aria-label="Map centered on Veliko Tarnovo, Bulgaria"
>
```
→ Replace with:
```tsx
const BULGARIA_CENTER: [number, number] = [42.7, 25.5];

<div
  className="h-screen w-screen"
  aria-label="Bulgaria weather map showing current temperatures for 28 district cities"
>
  <MapContainer
    center={BULGARIA_CENTER}
    zoom={7}
    scrollWheelZoom
    className="h-full w-full"
  >
```
- **PITFALL §7:** `center` and `zoom` are immutable after mount — set correctly from the start (`[42.7, 25.5]`, zoom `7`).

**TileLayer — replace OSM with CartoDB Positron** (analog line 37–39):
```typescript
// ANALOG: src/weather-ui/src/components/WeatherMap.tsx  lines 37-39
<TileLayer
  attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
  url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
/>
```
→ Replace with:
```tsx
<TileLayer
  attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/">CARTO</a>'
  url="https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png"
/>
```

**Marker rendering — replace single Marker with 28 CityMarkers:**
```tsx
{/* Replace analog lines 41-58 (single Marker + Popup) with: */}
{cities?.map((city) => (
  <CityMarker key={city.nameEn} city={city} />
))}
```

---

### `src/components/CityMarker.tsx` (component, transform)

**Analog:** `src/weather-ui/src/components/WeatherMap.tsx` (Marker usage, lines 41–58) + `src/components/WeatherPopup.tsx` (temperature color logic, lines 31–36)

**No direct analog exists** for the memoized `L.divIcon` pattern — this is a new pattern in the codebase. Use the reference implementation from STACK.md and UI-SPEC.md.

**Imports pattern** — copy from WeatherMap.tsx analog, stripped down:
```typescript
// Derived from ANALOG: src/weather-ui/src/components/WeatherMap.tsx  lines 1-3
import { useMemo } from 'react';
import { Marker, Tooltip } from 'react-leaflet';
import L from 'leaflet';
import type { CityTemperature } from '../types/cityTemperature';
```
Note: `'leaflet/dist/leaflet.css'` is already imported in `BulgariaMap.tsx` — do not double-import.

**Temperature color function** — pattern from `WeatherPopup.tsx` lines 31–36 (adapt to Tailwind classes):
```typescript
// ANALOG: src/weather-ui/src/components/WeatherPopup.tsx  lines 31-36
function tempColor(tempC: number): string {
  if (tempC <= 0) return '#60a5fa';
  if (tempC <= 10) return '#93c5fd';
  if (tempC <= 20) return '#6ee7b7';
  if (tempC <= 30) return '#fbbf24';
  return '#f87171';
}
```
→ Rewrite for Tailwind class names per D-09 (locked color palette):
```typescript
function getTempColorClass(temp: number): string {
  if (temp <= 0)  return 'bg-blue-500';
  if (temp <= 15) return 'bg-teal-500';
  if (temp <= 25) return 'bg-green-500';
  if (temp <= 35) return 'bg-orange-500';
  return 'bg-red-500';
}
```

**`createTempIcon` pure function** — from STACK.md reference + UI-SPEC §2:
```typescript
function createTempIcon(temp: number): L.DivIcon {
  const colorClass = getTempColorClass(temp);
  const label = isNaN(temp) ? '—' : `${temp}°C`;  // badge render guard (UI-SPEC data contract)
  return L.divIcon({
    className: '',   // ← CRITICAL: clears leaflet-div-icon default white box (D-07)
    html: `<div class="${colorClass} text-white text-[13px] font-bold
                        px-2 py-1 rounded shadow-md whitespace-nowrap
                        leading-none select-none">
             ${label}
           </div>`,
    iconSize:   [52, 28],   // UI-SPEC §2 / STACK.md
    iconAnchor: [26, 14],   // centers badge on coordinate
  });
}
```

**Memoized CityMarker component** — **PITFALL §6: DivIcon MUST be memoized**:
```tsx
// PITFALL §6 prevention — useMemo keyed to temperatureC
export function CityMarker({ city }: { city: CityTemperature }) {
  const roundedTemp = Math.round(city.temperatureC ?? NaN);  // D-08: integer display
  const icon = useMemo(
    () => createTempIcon(roundedTemp),
    [roundedTemp]   // recreate only when temperature changes
  );

  return (
    <Marker position={[city.latitude, city.longitude]} icon={icon}>
      <Tooltip direction="top" permanent={false} opacity={0.9}>
        {city.nameEn}
      </Tooltip>
    </Marker>
  );
}
```

**Pitfall guards:**
- **PITFALL §6:** `icon` must come from `useMemo`, never created inline in JSX.
- **PITFALL §13:** Use `city.latitude` / `city.longitude` (canonical coords from API response), not snapped Open-Meteo coordinates — these are already the canonical values because the backend maps by index and uses its own city list.

---

### `src/App.tsx` (rewrite — component, request-response)

**Analog:** `src/weather-ui/src/App.tsx` — **entire file is the structural template**

**Imports pattern** (analog lines 1–3):
```typescript
// ANALOG: src/weather-ui/src/App.tsx  lines 1-3
import { useWeatherData } from './hooks/useWeatherData';
import { WeatherMap } from './components/WeatherMap';
import './App.css';
```
→ Replace with:
```typescript
import { useCityTemperatures } from './hooks/useCityTemperatures';
import { BulgariaMap } from './components/BulgariaMap';
// No App.css import — D-04 deletes App.css; all styles via Tailwind v4 utilities
```

**Hook destructuring** (analog line 6):
```typescript
// ANALOG: src/weather-ui/src/App.tsx  line 6
const { data, loading, error } = useWeatherData();
```
→ Replace with:
```typescript
const { data, loading, error, retry } = useCityTemperatures();
```

**JSX structure** — replace header/main/footer chrome (analog lines 8–31) with full-viewport layout per D-03 and UI-SPEC layout contract:
```tsx
// NEW App.tsx structure — full-viewport, zero chrome (D-03)
return (
  <div className="relative w-screen h-screen overflow-hidden">
    {/* Map: absolute inset-0, always rendered so tiles load immediately (D-11) */}
    <BulgariaMap cities={data} />

    {/* Loading overlay: absolute inset-0, z-1000, visible during fetch (D-11, D-13) */}
    {loading && (
      <div className="absolute inset-0 z-[1000] bg-black/45 flex items-center justify-center">
        <div
          className="w-10 h-10 rounded-full border-4 border-white/20 border-t-white animate-spin"
          role="status"
          aria-label="Loading temperature data…"
        />
      </div>
    )}

    {/* Error overlay: absolute inset-0, z-1000, visible on error (D-12, D-13) */}
    {error && (
      <div className="absolute inset-0 z-[1000] bg-black/45 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-2xl p-6 max-w-sm w-full flex flex-col gap-4 border-l-4 border-red-500">
          <span className="text-3xl">⚠️</span>
          <h2 className="text-lg font-bold text-gray-900">Unable to load weather data</h2>
          <p className="text-sm text-gray-600 leading-relaxed">
            The weather service is unavailable. Make sure the backend is running at localhost:5055, then try again.
          </p>
          <button
            onClick={retry}
            className="bg-blue-600 hover:bg-blue-700 text-white text-sm font-bold px-4 py-2 rounded"
          >
            Try again
          </button>
        </div>
      </div>
    )}
  </div>
);
```

**Pitfall guard (PITFALLS.md §14):** Remove `VELIKO TARNOVO` header copy entirely — no header chrome in this phase.

---

### `src/index.css` (modify — style/config)

**Analog:** `src/weather-ui/src/index.css` — existing file is modified in place

**Current content** (lines 1–33 — entire file):
```css
/* ANALOG: src/weather-ui/src/index.css  lines 1-33 */
@tailwind base;
@tailwind components;
@tailwind utilities;

*, *::before, *::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

:root { /* custom properties ... */ }

html, body, #root {
  height: 100%;
  width: 100%;
  overflow: hidden;
}
```

**New file — full replacement:**
```css
/* Tailwind v4 CSS-first import (D-01, D-02) */
@import "tailwindcss";

/* Box-sizing reset — preserve from analog */
*, *::before, *::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

/* Full-viewport root (UI-SPEC Layout Contract) */
html, body, #root {
  height: 100%;
  width: 100%;
  overflow: hidden;
}
```

**Key changes:**
- Replace `@tailwind base/components/utilities` → `@import "tailwindcss"` (D-02).
- Delete `App.css` entirely — do not import it anywhere.
- Delete all `:root` custom properties (they were for the old dark theme; new design uses Tailwind utilities directly).
- Keep `html, body, #root` reset from analog lines 21–25 — **required** for full-viewport map (UI-SPEC Layout Contract).
- System font stack is set via Tailwind's `font-sans` default — no `font-family` declaration needed.

---

### `vite.config.ts` (modify — config)

**Analog:** `src/weather-ui/vite.config.ts` — existing file, lines 1–6 (entire file):
```typescript
// ANALOG: src/weather-ui/vite.config.ts  lines 1-6
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
})
```

**New file — replace with Tailwind v4 plugin + Vite proxy:**
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'   // D-02: replaces PostCSS pipeline

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),   // D-02: Tailwind v4 Vite integration
  ],
  server: {
    proxy: {
      '/api': 'http://localhost:5055'   // PITFALL §4: avoids HTTP→HTTPS redirect + CORS
    }
  }
})
```

**Notes:**
- With the proxy in place, the hook URL changes from `http://localhost:5055/api/cities/temperatures` to `/api/cities/temperatures` (relative). **Update the hook URL accordingly.**
- `@tailwindcss/vite` replaces `postcss` + `autoprefixer` — those packages will be uninstalled from `package.json`.

---

### `package.json` (modify — config)

**Analog:** `src/weather-ui/package.json` — existing file

**Current devDependencies** (lines 18–27):
```json
// ANALOG: src/weather-ui/package.json  lines 18-27
"devDependencies": {
  "@types/leaflet": "^1.9.14",
  "@types/react": "^19.1.2",
  "@types/react-dom": "^19.1.2",
  "@vitejs/plugin-react": "^4.4.1",
  "autoprefixer": "^10.4.21",
  "postcss": "^8.5.3",
  "tailwindcss": "^3.4.17",
  "typescript": "~5.8.3",
  "vite": "^6.3.3"
}
```

**Target state:**
```json
"devDependencies": {
  "@tailwindcss/vite": "^4.2.4",      // ← ADD (D-02)
  "@types/leaflet": "^1.9.14",
  "@types/react": "^19.1.2",
  "@types/react-dom": "^19.1.2",
  "@vitejs/plugin-react": "^4.4.1",
  // "autoprefixer": removed           // ← REMOVE (D-02)
  // "postcss": removed                // ← REMOVE (D-02)
  "tailwindcss": "^4.2.4",            // ← UPGRADE from 3.4.17 (D-01)
  "typescript": "~5.8.3",
  "vite": "^6.3.3"
}
```

**Install command (from STACK.md):**
```bash
cd src/weather-ui
npm install tailwindcss@latest @tailwindcss/vite@latest
npm uninstall autoprefixer postcss
```

---

## Shared Patterns

### Cancel-on-unmount (`cancelled` flag)
**Source:** `src/weather-ui/src/hooks/useWeatherData.ts` lines 16–38
**Apply to:** `useCityTemperatures.ts`
```typescript
// SHARED: Cancel-on-unmount pattern — prevents setState after component unmount
useEffect(() => {
  let cancelled = false;
  const fetchData = async () => {
    try {
      // ... fetch logic ...
      if (!cancelled) setData(json);
    } catch (err) {
      if (!cancelled) setError(/* ... */);
    } finally {
      if (!cancelled) setLoading(false);
    }
  };
  fetchData();
  return () => { cancelled = true; };
}, [/* dependency */]);
```

### Leaflet Default Icon Fix
**Source:** `src/weather-ui/src/components/WeatherMap.tsx` lines 8–17
**Apply to:** `BulgariaMap.tsx`
```typescript
// SHARED: Vite bundler fix for Leaflet default marker images
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
});
```

### Error State Rendering
**Source:** `src/weather-ui/src/components/WeatherMap.tsx` lines 49–53
**Apply to:** `App.tsx` error overlay (pattern: conditional render of error state)
```tsx
// SHARED: Error state conditional render (analog uses className-based styling)
{error && (
  <div className="popup-state popup-error">
    <span>⚠️ {error}</span>
  </div>
)}
```
→ In `App.tsx`, this becomes the full error card overlay using Tailwind v4 utilities from UI-SPEC §5.

### DivIcon Memoization (new pattern — no codebase analog)
**Source:** PITFALLS.md §6 + UI-SPEC §2 + STACK.md reference implementation
**Apply to:** `CityMarker.tsx` — every marker component that creates `L.divIcon`
```tsx
// SHARED: Memoized DivIcon — prevents marker flicker on re-render (PITFALL §6)
const icon = useMemo(
  () => createTempIcon(Math.round(city.temperatureC ?? NaN)),
  [city.temperatureC]
);
```

### Tailwind v4 Entry Point
**Source:** `src/weather-ui/src/index.css` (to be replaced)
**Apply to:** `index.css`
```css
/* Tailwind v4: single directive replaces @tailwind base/components/utilities */
@import "tailwindcss";
```

### Full-Viewport Root Reset
**Source:** `src/weather-ui/src/index.css` lines 21–25
**Apply to:** `index.css` (keep verbatim)
```css
html, body, #root {
  height: 100%;
  width: 100%;
  overflow: hidden;
}
```

---

## No Analog Found

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `src/components/CityMarker.tsx` | component | transform | No memoized DivIcon component exists — new pattern. Use STACK.md reference + UI-SPEC §2. |

---

## Delete List (no pattern needed — remove entirely)

| File | Reason |
|---|---|
| `src/weather-ui/src/App.css` | All styles replaced by Tailwind v4 utilities (D-04) |
| `src/weather-ui/src/components/WeatherPopup.tsx` | 5-day forecast popup; incompatible with new data model (PITFALL §12) |
| `src/weather-ui/src/types/weather.ts` | `WeatherForecast` replaced by `CityTemperature` (PITFALL §5) |
| `src/weather-ui/tailwind.config.js` | Tailwind v4 uses CSS-first config — no JS config file (D-02) |
| `src/weather-ui/postcss.config.js` | Replaced by `@tailwindcss/vite` plugin (D-02) |

---

## Metadata

**Analog search scope:** `src/weather-ui/src/` (all subdirectories)
**Files scanned:** 7 existing source files (App.tsx, useWeatherData.ts, WeatherMap.tsx, WeatherPopup.tsx, weather.ts, index.css, vite.config.ts)
**Pattern extraction date:** 2026-04-28

**Pitfall cross-reference:**
| Pitfall | Affects | Mitigation captured in |
|---|---|---|
| §4 HTTPS redirect | `vite.config.ts` proxy | vite.config.ts pattern |
| §5 Data model overhaul | All src/ files | Delete list + new types |
| §6 DivIcon memoization | `CityMarker.tsx` | CityMarker useMemo pattern |
| §7 MapContainer immutability | `BulgariaMap.tsx` | center/zoom set correctly at mount |
| §11 Wrong API URL | `useCityTemperatures.ts` | URL updated to `/api/cities/temperatures` |
| §12 WeatherPopup obsolete | `WeatherPopup.tsx` | Delete list |
| §13 Snapped coordinates | `CityMarker.tsx` | Use `city.latitude`/`city.longitude` directly |
| §14 Hardcoded city name | `App.tsx` | No header chrome (D-03) |
