# Roadmap: Bulgaria Weather Map

## Overview

Two-phase build that mirrors the natural dependency boundary of the project: stand up the ASP.NET Core backend proxy first (all data originates there), then overhaul the React frontend to replace the old single-city view with a full-viewport Bulgaria map showing 28 live, color-coded temperature markers. The backend is a prerequisite — the frontend hook cannot be validated end-to-end without a live `/api/cities/temperatures` endpoint.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Backend API** - ASP.NET Core proxy endpoint returning live temperatures for all 28 Bulgarian district cities
- [ ] **Phase 2: Frontend Map** - React SPA full-rewrite: full-viewport Leaflet map with 28 color-coded temperature markers

## Phase Details

### Phase 1: Backend API
**Goal**: The `/api/cities/temperatures` endpoint is live, returning real Open-Meteo temperatures for all 28 Bulgarian district cities with 10-minute caching and correct CORS headers
**Depends on**: Nothing (first phase)
**Requirements**: API-01, API-02, API-03, API-04, API-05, API-06
**Success Criteria** (what must be TRUE):
  1. `GET http://localhost:5055/api/cities/temperatures` returns a JSON array of exactly 28 objects, each containing `name`, `latitude`, `longitude`, and `temperatureC` — verified via curl or Swagger
  2. Returned `temperatureC` values are real current temperatures (not mock/random data) sourced from Open-Meteo's bulk API in a single HTTP request
  3. Repeated calls within 10 minutes return the cached result; a call after the TTL expires triggers a fresh Open-Meteo fetch
  4. A `fetch('http://localhost:5055/api/cities/temperatures')` from the Vite dev server at `http://localhost:5173` succeeds — no CORS error and no HTTPS redirect loop in browser DevTools
**Plans**: TBD

Plans:
- [ ] 01-01: TBD

### Phase 2: Frontend Map
**Goal**: Users can see current temperatures for all 28 Bulgarian district cities on a full-viewport interactive Leaflet map — with color-coded markers, city name tooltips, loading/error states, and zero distracting chrome
**Depends on**: Phase 1
**Requirements**: MAP-01, MAP-02, MAP-03, MAP-04, UI-01, UI-02, UI-03, UI-04
**Success Criteria** (what must be TRUE):
  1. User sees a full-viewport Leaflet map centered on Bulgaria with all 28 district city markers visible at the default zoom level (zoom 7, CartoDB Positron tiles)
  2. Each marker displays the current temperature in °C as a color-coded badge — cold temperatures show blue/teal, warm show green, hot show orange/red — and the color changes reflect real temperature range thresholds
  3. Hovering over any marker shows the city name as a tooltip
  4. While the API call is in flight, a loading indicator is visible; if the backend request fails, a clear error message is shown instead of a broken map
  5. The map is the only major element on screen — no sidebars, panels, or decorative UI that competes with the map for attention
**Plans**: TBD
**UI hint**: yes

Plans:
- [ ] 02-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Backend API | 0/? | Not started | - |
| 2. Frontend Map | 0/? | Not started | - |
