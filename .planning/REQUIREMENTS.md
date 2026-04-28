# Requirements — Bulgaria Weather Map

## v1 Requirements

### Backend / API

- [ ] **API-01**: User receives a JSON array of all 28 district cities with current temperature via `GET /api/cities/temperatures` (returns: cityName, lat, lng, temperatureC)
- [ ] **API-02**: Backend fetches live weather data from Open-Meteo in a single bulk HTTP request for all 28 cities (via `IHttpClientFactory`)
- [ ] **API-03**: Backend caches Open-Meteo responses with `IMemoryCache` — 10-minute absolute TTL (aligns with Open-Meteo's 15-min update cadence)
- [ ] **API-04**: All 28 Bulgarian district city coordinates are defined in a single `BulgarianCities.cs` static class (single source of truth — returned in API response so frontend needs no coordinate list)
- [ ] **API-05**: `UseHttpsRedirection` is guarded to development environment only — prevents HTTP fetch failures when running under the `http` launch profile
- [ ] **API-06**: CORS policy allows requests from `http://localhost:5173` (Vite dev server)

### Map / Markers

- [ ] **MAP-01**: User sees an interactive Leaflet map centered on Bulgaria (`[42.7, 25.5]`, zoom 7) with CartoDB Positron tiles (clean, minimal tile style)
- [ ] **MAP-02**: User sees 28 markers on the map — one per district city — each displaying the current temperature in °C using a custom `L.divIcon` HTML badge (memoized to prevent flicker)
- [ ] **MAP-03**: Each marker badge is color-coded by temperature range (e.g., ≤0°C blue, 1–15°C teal, 16–25°C green, 26–35°C orange, >35°C red)
- [ ] **MAP-04**: User sees the city name when hovering over any marker (Leaflet `<Tooltip>` component)

### Frontend / UX

- [ ] **UI-01**: React 19 SPA project at `src/weather-ui/` (Vite + TypeScript + Tailwind CSS + react-leaflet) starts with `npm run dev` on `http://localhost:5173`
- [ ] **UI-02**: User sees a loading indicator while temperature data is being fetched from the backend
- [ ] **UI-03**: User sees a clear error message if the backend request fails
- [ ] **UI-04**: The map occupies the full viewport as the hero element — minimal chrome, no sidebars or distracting UI elements

## v2 Requirements (deferred)

- "Last updated" timestamp showing when data was last fetched from Open-Meteo
- Temperature unit toggle (°C / °F)
- Click-to-expand popup with additional weather details (humidity, wind speed)

## Out of Scope

- 5-day weather forecast — user explicitly chose current temperature only
- Popup with humidity/wind/pressure details — temperature badge is sufficient for v1
- Dark/atmospheric visual theme — user chose minimal/clean design
- User authentication — public application, no login required
- Mobile-native app — web-only
- Historical weather data — real-time current temperature only

## Traceability

| REQ-ID | Phase |
|--------|-------|
| API-01 | Phase 1 |
| API-02 | Phase 1 |
| API-03 | Phase 1 |
| API-04 | Phase 1 |
| API-05 | Phase 1 |
| API-06 | Phase 1 |
| MAP-01 | Phase 2 |
| MAP-02 | Phase 2 |
| MAP-03 | Phase 2 |
| MAP-04 | Phase 2 |
| UI-01  | Phase 2 |
| UI-02  | Phase 2 |
| UI-03  | Phase 2 |
| UI-04  | Phase 2 |
