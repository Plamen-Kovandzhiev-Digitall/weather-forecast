# Phase 2: Frontend Map - Context

**Gathered:** 2026-04-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Full rewrite of `src/weather-ui/` — React 19 SPA showing all 28 Bulgarian district cities as color-coded temperature badges on a full-viewport interactive Leaflet map. The backend API (`/api/cities/temperatures`) is already live from Phase 1. This phase is purely frontend.

All existing `src/weather-ui/src/` files are incompatible with the new API shape and must be deleted and rewritten from scratch. Do not patch the old code.

</domain>

<decisions>
## Implementation Decisions

### Tailwind version
- **D-01:** Upgrade from v3.4.17 to v4 (latest) during the rewrite. Clean slate since all CSS is being rewritten anyway.
- **D-02:** Remove `postcss.config.js`, `autoprefixer`, and `tailwind.config.js`. Use `@tailwindcss/vite` plugin in `vite.config.ts` and `@import "tailwindcss"` in `index.css`.

### App chrome
- **D-03:** Pure full-viewport — `MapContainer` fills `100vw × 100vh`, zero additional header or footer chrome. Leaflet's built-in attribution control (bottom-right) is the only persistent UI element outside the map.
- **D-04:** Delete `App.css` and all existing component/hook/type files; replace with the new implementation.

### Map setup
- **D-05:** Map centered on Bulgaria: `[42.7, 25.5]`, zoom level 7.
- **D-06:** Tile provider: CartoDB Positron (`https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png`). Attribution: `&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/">CARTO</a>`.

### Temperature markers
- **D-07:** Custom `L.divIcon` badge for each city marker. `className: ''` to clear Leaflet's default `leaflet-div-icon` styles so Tailwind controls all styling.
- **D-08:** Temperature displayed as **integer** — round to nearest whole number (e.g., `Math.round(temperatureC)` → `19°C`). No decimals on badges.
- **D-09:** Color coding by temperature range:
  - ≤ 0°C → blue
  - 1–15°C → teal
  - 16–25°C → green
  - 26–35°C → orange
  - > 35°C → red
- **D-10:** City name shown on marker hover via Leaflet `<Tooltip>` component (permanent: false, direction: top or auto).

### Loading & error states
- **D-11:** Loading: spinner overlay centered over the (already-rendered) map tiles while `fetch` is in-flight. Map tiles are visible beneath the overlay.
- **D-12:** Error: centered card overlay on top of the map (modal-style, not a banner). Shows a human-readable error message with a retry option or instructions.
- **D-13:** Both overlays are absolutely positioned over the map and do not affect its layout.

### Data fetching
- **D-14:** Hook fetches from `http://localhost:5055/api/cities/temperatures` on mount (single fetch, no polling). `cancelled` flag pattern to avoid state updates after unmount (already established in `useWeatherData.ts`).
- **D-15:** Response type: `CityTemperature[]` matching the backend shape `{ nameNative, nameEn, latitude, longitude, temperatureC }`.

### the agent's Discretion
- Exact spinner component (CSS animation vs SVG vs emoji)
- Error card copy and button text
- Marker badge size, font weight, border radius, shadow — must be readable at zoom 7
- Whether to memoize `createTempIcon` per temperature value (use `useMemo` or a pure function)
- `iconSize` and `iconAnchor` values for the DivIcon (should center the badge on coordinates)
- Whether to keep the Leaflet default icon fix (needed if any fallback `Marker` without custom icon is used)

</decisions>

<specifics>
## Specific Ideas

- The STACK.md has a concrete `createTempIcon` reference implementation using `L.divIcon` with Tailwind classes and `iconSize: [52, 28]` / `iconAnchor: [26, 14]` — use as a starting point.
- The existing `useWeatherData.ts` cancel-on-unmount pattern should be preserved in the new hook.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope & requirements
- `.planning/REQUIREMENTS.md` — Full requirement list: MAP-01–04, UI-01–04, with exact acceptance criteria
- `.planning/ROADMAP.md` — Phase 2 success criteria (5 items)

### Stack & integration
- `.planning/research/STACK.md` — Confirmed library versions, Tailwind v4 migration steps, `L.divIcon` reference implementation, TypeScript config notes
- `.planning/research/PITFALLS.md` — Known pitfalls to avoid during frontend implementation

### Backend contract
- `src/WeatherForecast/Models/CityTemperature.cs` — Exact DTO shape: `nameNative`, `nameEn`, `latitude`, `longitude`, `temperatureC` (now `double?`)
- `src/WeatherForecast/Controllers/CitiesController.cs` — Route: `GET /api/cities/temperatures`

### Existing frontend (to delete and rewrite)
- `src/weather-ui/src/` — Current files to replace. Pattern reference only — do not copy logic, update the hook URL and API shape.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Patterns
- `useWeatherData.ts` cancel-on-unmount pattern (`cancelled` flag in `useEffect`) — preserve this pattern in the new hook
- `WeatherMap.tsx` Leaflet default icon fix (lines 12-17) — keep if using any non-DivIcon markers; harmless to include

### Integration Points
- New hook fetches `GET http://localhost:5055/api/cities/temperatures` → `CityTemperature[]`
- `MapContainer` replaces `WeatherMap`'s current single-marker implementation
- `App.tsx` becomes a thin wrapper: fetch state → loading/error overlays + map

### What to Delete
- `App.css` — all styles replaced by Tailwind v4 utility classes
- `components/WeatherPopup.tsx` — popup was for the old 5-day forecast; no longer needed
- `types/weather.ts` — `WeatherForecast` type replaced by `CityTemperature`
- Existing `WeatherMap.tsx` and `useWeatherData.ts` — rewrite, don't patch

</code_context>

<deferred>
## Deferred Ideas

- "Last updated" timestamp on the map — v2 requirement
- Temperature unit toggle (°C / °F) — v2 requirement
- Click popup with humidity/wind details — explicitly out of scope (PROJECT.md)
- Dark/atmospheric map tile theme — user chose minimal/clean (PROJECT.md)

</deferred>

---

*Phase: 02-frontend-map*
*Context gathered: 2026-04-28*
