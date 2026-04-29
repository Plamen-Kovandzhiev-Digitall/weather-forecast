# Project Research Summary

**Project:** Bulgaria Weather Temperature Map  
**Domain:** Real-time geographic weather visualization (read-only SPA + API proxy)  
**Researched:** 2026-04-27  
**Confidence:** HIGH — all critical decisions verified via live API calls, npm registry, and existing codebase inspection

## Executive Summary

This is a focused, single-purpose web application: show the current temperature for all 28 Bulgarian district cities on an interactive map. The entire stack is already chosen and largely installed — React 19 + Vite + TypeScript + Tailwind CSS + react-leaflet on the frontend, ASP.NET Core 8 on the backend. The project is essentially a migration from a toy single-city forecast view to a real Bulgaria-wide temperature map, powered by Open-Meteo's free bulk API (no key required). The scope is deliberately narrow and the technical complexity is low — most of the work is wiring together patterns that already exist in the codebase.

The recommended approach is a clean two-phase build: stand up the backend `/cities/temperatures` endpoint first (Open-Meteo proxy with IMemoryCache), then overhaul the frontend to replace the old single-city view with 28 color-coded temperature markers on a CartoDB Positron map. All 28 city coordinates are hardcoded on the backend; the frontend is kept thin and receives `{name, lat, lon, temperatureC}` per city. No new npm packages or NuGet packages are needed beyond what is already installed.

The primary risks are subtle integration traps: Open-Meteo returns a positional array (must join by index, not by coordinate), `UseHttpsRedirection` in Program.cs will silently break HTTP fetches from Vite dev server, and the existing frontend data model is **completely incompatible** with the new API shape and must be fully replaced — not patched. All three risks have clear, low-effort mitigations documented in detail in PITFALLS.md.

---

## Key Findings

### Recommended Stack

The entire stack is already in place. No new packages are required.

**Core technologies:**
- **React 19 + Vite 6 + TypeScript 5.8** — frontend runtime, already installed; `moduleResolution: "bundler"` is correctly set for Vite 6
- **react-leaflet 5.0.0 + leaflet 1.9.4** — map rendering; v5.0.0 is explicitly React-19-compatible (peer dep `react: "^19.0.0"`)
- **Tailwind CSS 3.4.17** — utility styling, already installed; optional upgrade to v4.2.4 possible but not required
- **ASP.NET Core 8 + IHttpClientFactory + IMemoryCache** — backend proxy; all built-in, zero new NuGet packages needed
- **Open-Meteo API** — free, no key, bulk coordinate support (up to 100 cities in one request), 15-min data refresh cadence

> See [STACK.md](./STACK.md) for full version matrix, Open-Meteo bulk request pattern, and Tailwind v3→v4 migration guidance.

### Expected Features

**Must have (table stakes):**
- Interactive Leaflet map of Bulgaria, centered at `[42.7, 25.5]` zoom 7 — all 28 cities visible at once
- 28 city markers using custom `DivIcon` (HTML badge), temperature `°C` displayed directly on map
- City name identifiable (Leaflet `<Tooltip>` on hover or permanent at zoom ≥ 8)
- Loading overlay while temperatures fetch (map tiles load independently, overlay signals data state)
- Non-blocking error banner (map stays visible; banner shows API failure + retry)
- Readable markers at zoom 7 (font ≥ 14px, contrast ≥ 4.5:1)

**Should have (high-value, low-effort):**
- Temperature color coding: blue (≤ 0°C) → light blue (1–10) → green (11–20) → orange (21–28) → red (> 28) — 10 lines of TS, instant readability gain
- "Last updated" timestamp in header — builds trust, 3 lines of code
- Auto-refresh every 10–15 min — aligns with Open-Meteo update cadence
- CartoDB Positron map tiles (muted, markers pop) over OSM default

**Defer to v2+:**
- 5-day forecast, weather condition icons, °C/°F toggle, dark theme, popups with humidity/wind, search, geolocation, PWA, historical data — all explicitly excluded by product decision

> See [FEATURES.md](./FEATURES.md) for full UX patterns, color scale, and layout specification.

### Architecture Approach

The architecture is a straightforward three-tier proxy: Browser → ASP.NET Core → Open-Meteo. The backend issues a **single bulk HTTP request** for all 28 cities, caches the result for 10 minutes (IMemoryCache), and returns a flat JSON array. The React SPA fetches that array once on load, renders a `MapContainer` centered on Bulgaria, and places 28 `TemperatureMarker` components (each using a memoized `DivIcon`). City coordinates are owned by the backend (`BulgarianCities.cs` static list) and returned in the API response — the frontend has no coordinate list of its own.

**Major components:**

Backend:
1. `BulgarianCities.cs` — static list of 28 `{Name, Lat, Lon}` records (the foundation)
2. `OpenMeteoClient` — wraps IHttpClientFactory, builds bulk URL, deserializes array response
3. `CityTemperatureService` — IMemoryCache check → OpenMeteoClient → zip by index → cache result
4. `CitiesController` — thin controller exposing `GET /api/cities/temperatures`

Frontend:
5. `useCityTemperatures` hook — `fetch` → `{ data, loading, error }`
6. `WeatherMap.tsx` — `MapContainer` at Bulgaria center/zoom, renders 28 markers
7. `TemperatureMarker.tsx` — per-city `DivIcon` with `useMemo`, color-coded badge

**Build order:** Backend steps 1–4 must complete before the frontend hook can be validated end-to-end. Frontend types and `TemperatureMarker` can be built in parallel with the backend.

> See [ARCHITECTURE.md](./ARCHITECTURE.md) for complete data flow, DI registration code, and TypeScript types.

### Critical Pitfalls

1. **Open-Meteo positional array — match by index, not coordinate** — Open-Meteo snaps coords to ~7 km grid; returned `latitude`/`longitude` will NOT match input. Always use `cities.Zip(openMeteoArray, ...)` by index. Never match by float coordinate lookup.

2. **`UseHttpsRedirection` silently breaks HTTP frontend fetches** — Program.cs issues a 307 redirect; Vite dev server fetches plain HTTP to port 5055; redirect goes to `https://localhost:443` (wrong port). Fix: wrap in `if (!app.Environment.IsDevelopment())` or configure a Vite proxy.

3. **Wrong Open-Meteo parameter: `current_weather` vs `current`** — Old API uses `current_weather=true`; current API uses `current=temperature_2m`. Using the old form returns no `current` key → all markers show `NaN°C`. Always use `&current=temperature_2m`.

4. **Full frontend data model overhaul required** — Existing `types/weather.ts`, `useWeatherData.ts`, `WeatherMap.tsx`, `WeatherPopup.tsx` are built for the old single-city forecast shape. These must be **deleted and rewritten**, not patched. Loose typing (`as any`) to silence TS errors will allow wrong data to silently reach the map.

5. **DivIcon must be memoized (`useMemo`)** — Creating `L.divIcon(...)` inline in the render function causes Leaflet to tear down and recreate all 28 markers on every React re-render → visible flicker. Wrap in `useMemo([tempC])` inside a `TemperatureMarker` component.

> See [PITFALLS.md](./PITFALLS.md) for 15 pitfalls including moderate (MapContainer immutability, CORS port dependency, HttpClient timeout) and minor (hardcoded old URL, obsolete header copy).

---

## Implications for Roadmap

### Phase 1: Backend API Endpoint

**Rationale:** All frontend features depend on `GET /api/cities/temperatures`. Nothing on the frontend can be validated end-to-end without it. Build data → service → API in dependency order.  
**Delivers:** Working endpoint returning `[{name, latitude, longitude, temperatureC}]` for all 28 cities, with 10-min cache.  
**Addresses:** API-01, API-02, CORS-01  
**Avoids:**
- Socket exhaustion → use `IHttpClientFactory`
- Wrong API parameter → use `current=temperature_2m`
- Index mismatch → use `cities.Zip(response)` by array position
- Null crash → use nullable DTO properties + null-check
- HTTPS redirect breaking fetch → gate `UseHttpsRedirection` behind `!IsDevelopment()`

**Key tasks:** `BulgarianCities.cs` (28 coords) → `OpenMeteoClient` → `CityTemperatureService` (cache) → `CitiesController` → `Program.cs` DI wiring → smoke test with curl/Swagger

### Phase 2: Frontend Map & Markers

**Rationale:** With the backend endpoint live, the entire frontend can be built and validated against real data. The existing frontend is a full replace, not an extension — treat it as greenfield within the existing React scaffold.  
**Delivers:** Full-viewport Bulgaria map with 28 color-coded temperature markers, loading overlay, error banner, city name tooltips, "last updated" timestamp.  
**Addresses:** MAP-01, MAP-02, MAP-03, UI-01, UI-02, UI-03  
**Avoids:**
- Wrong MapContainer center → set `[42.7, 25.5]` zoom `7` from the start (immutable after mount)
- DivIcon flicker → `useMemo` in `TemperatureMarker`
- Stale frontend types → delete old files, rewrite from scratch
- Marker positioning with snapped coords → use canonical city coords from API response

**Key tasks:** Delete old types/components → `CityTemperature` TS interface → `useCityTemperatures` hook → `TemperatureMarker` (DivIcon + color scale) → `WeatherMap` (MapContainer + 28 markers) → `App.tsx` header → loading/error states

### Phase Ordering Rationale

- Backend-first is mandatory: the frontend hook requires a live endpoint for end-to-end validation
- TypeScript types and `TemperatureMarker` component can be developed in parallel with the backend (no runtime dependency until integration)
- The frontend is a full rewrite of existing files, not incremental additions — doing it in one phase avoids partial states where old and new code conflict
- Both phases are small: backend ~5 new files, frontend ~5 replaced/new files; a two-phase structure matches the natural dependency boundary

### Research Flags

Phases with standard, well-documented patterns — **skip `/gsd-research-phase`:**
- **Phase 1 (Backend):** IHttpClientFactory, IMemoryCache, System.Text.Json are standard ASP.NET Core 8 patterns with official docs. Open-Meteo bulk request format verified via live API call. No unknowns.
- **Phase 2 (Frontend):** react-leaflet DivIcon pattern is well-documented. Color scale is pure TS. Hook pattern already exists in codebase as a template. No unknowns.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All versions verified via npm registry; Open-Meteo bulk API verified via live curl test; existing project files inspected directly |
| Features | HIGH | Scope is explicitly defined in PROJECT.md; UX patterns cross-referenced against Windy.com, Meteoblue, Ventusky conventions |
| Architecture | HIGH | Data flow verified end-to-end via live Open-Meteo test; existing codebase patterns confirmed; build order derived from direct dependency analysis |
| Pitfalls | HIGH | Pitfalls sourced from existing codebase code review (Program.cs, useWeatherData.ts, vite.config.ts) + live API response shape confirmation |

**Overall confidence:** HIGH

### Gaps to Address

- **Tailwind v3 → v4 migration:** Research recommends v4 but flags it as optional. Decision should be made explicitly at the start of Phase 2 (not mid-phase). If v3 stays, no action needed; if upgrading, follow the 4-step migration in STACK.md before writing any new CSS.
- **`start.ps1` dev setup:** Script may not start both backend and frontend. Verify and update before Phase 1 testing — a broken dev script will waste time during integration.
- **28 city coordinate list:** Coordinates will be hardcoded in `BulgarianCities.cs`. Research confirms the approach but the actual WGS84 values for all 28 cities need to be sourced (Wikipedia / Bulgarian administrative database) during Phase 1 implementation.

---

## Sources

### Primary (HIGH confidence)
- Open-Meteo live API test (2026-04-27) — bulk request format, `location_id` behavior, coordinate snapping, `current.temperature_2m` response shape
- `npm info react-leaflet` (2026-04-27) — v5.0.0 confirmed latest, peer deps `react: "^19.0.0"`
- `npm info tailwindcss` (2026-04-27) — v4.2.4 confirmed latest, v3.4.x is `v3-lts`
- Existing project source: `src/WeatherForecast/Program.cs`, `src/weather-ui/package.json`, `tsconfig.json`, `vite.config.ts`, `useWeatherData.ts`

### Secondary (MEDIUM confidence)
- react-leaflet official docs (Context7) — MapContainer immutability, DivIcon API, Tooltip API
- Open-Meteo docs `https://open-meteo.com/en/docs` — multi-location parameter specification
- CartoDB basemaps `https://github.com/CartoDB/basemap-styles` — Positron tile URL, no API key required

### Tertiary (pattern inference)
- Weather map UX conventions (Windy.com, Meteoblue, Ventusky, weather.com) — temperature color scale, badge marker design, layout patterns

---
*Research completed: 2026-04-27*  
*Ready for roadmap: yes*
