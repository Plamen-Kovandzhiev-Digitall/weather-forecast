# Features Research — Bulgaria Weather Map

**Domain:** Real-time temperature map (single-purpose, geographic, read-only)
**Researched:** 2026-04-27
**Scope:** 28 Bulgarian district cities, current temperature only, minimal/clean UI

---

## Table Stakes (must have)

Features users expect from any weather map. Missing → product feels broken or incomplete.

| Feature | Why Users Expect It | Complexity | Notes |
|---------|---------------------|------------|-------|
| **Map of Bulgaria centered correctly** | The entire purpose — wrong center or zoom = broken | Low | Center ~42.7°N, 25.5°E, zoom 7–8 shows all 28 cities |
| **All 28 city markers visible simultaneously** | Core value prop: at-a-glance view of the whole country | Low | Must fit within default viewport without scrolling |
| **Temperature °C displayed directly on marker** | User's explicit choice; users cannot click-to-reveal on a glance map | Low | Custom `DivIcon` (HTML badge), not default Leaflet pin |
| **City name identifiable** | Without knowing which dot is which city, the map is useless | Low | Leaflet `Tooltip` (permanent or on-hover); or label below badge |
| **Loading state** | Data takes 100–500ms to fetch; blank map with no feedback = broken | Low | Full-map spinner or skeleton overlay; already in UI-03 |
| **Error state** | Open-Meteo or ASP.NET proxy can fail; silent failure = user confusion | Low | Error banner or centered message; already in UI-03 |
| **Interactive map (zoom + pan)** | Any Leaflet map is interactive by default; removing it would feel broken | Low | Free with react-leaflet; do not disable |
| **Readable marker at default zoom** | If the °C badge is too small at zoom 7–8, core data is illegible | Low | Font size ≥14px, contrast ratio ≥4.5:1 on badge background |

---

## Differentiators (nice to have for v1)

Features that add value without changing the minimal scope. All are LOW complexity.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Temperature color coding on badges** | Instant visual "hot/cold" gradient — industry standard for weather maps; users parse data faster | Low | CSS class map: `≤0°C` → blue, `1–10` → light blue, `11–20` → green, `21–30` → orange, `>30` → red. Pure CSS/TS, no extra deps |
| **"Last updated" timestamp** | Open-Meteo returns hourly data; showing "Updated 14:00" builds trust in data freshness | Low | Track `Date.now()` at fetch completion, render in footer or corner overlay |
| **Auto-refresh every 10–15 min** | Temperatures change; a stale map is misleading if left open | Low | `setInterval` + re-fetch; re-render markers on new data. 15 min aligns with Open-Meteo hourly cadence |
| **City name tooltip on hover** | City names on every marker clutters a 28-marker map; hover reveals name without visual noise | Low | react-leaflet `<Tooltip>` — 5 lines of code |
| **Responsive layout (mobile)** | Maps are commonly viewed on phone; Tailwind makes this trivial | Low | `h-screen w-full` on map container; markers are tap-friendly by default |

---

## Anti-Features (explicitly NOT for v1)

Things to deliberately skip. Deferring these is a product decision, not a technical limitation.

| Anti-Feature | Why Defer / Exclude | What to Do Instead |
|--------------|--------------------|--------------------|
| **5-day forecast** | User's explicit exclusion; adds backend complexity (different Open-Meteo params), UI complexity (popups/panels), and contradicts "map is hero" layout | — Excluded by product decision |
| **Popup with weather details (humidity, wind, UV, etc.)** | User's explicit exclusion; contradicts temperature-only marker design | — Excluded by product decision |
| **Dark / atmospheric theme** | User's explicit exclusion; contradicts minimal/clean direction | Use light map tiles (OpenStreetMap default or CartoDB Positron) |
| **Authentication / login** | No user data, no personalization, no private data — auth adds zero value | Public read-only app |
| **Weather condition icons (sunny ☀️ / cloudy ⛅)** | Requires additional API field (`weathercode`), icon assets, marker redesign; adds visual noise to a number-first layout | — Defer to v2 if ever |
| **°C / °F toggle** | User said °C only; adds state management, marker re-render logic | — Excluded by product decision |
| **Search / filter cities** | Only 28 cities, all visible at once; search solves a problem that doesn't exist here | — Not needed at this scale |
| **Geolocation ("show my city")** | Adds browser permission prompt; distracts from whole-country view | — Map already shows all cities |
| **Historical data / charts** | Entirely different product; requires time-series storage | — Not in scope |
| **Multiple weather layers (wind, rain radar)** | Complex rendering (WMS tiles or Leaflet overlays), multiple API calls, UI controls | — Defer to v2 |
| **PWA / offline mode** | Weather data must be live; offline cache is stale by definition | — No offline value here |
| **Notifications / weather alerts** | Requires push infrastructure, user accounts, Bulgarian meteorology data source | — Out of scope |
| **Share / embed functionality** | URL state management, iframe sandboxing; adds complexity without v1 value | — Defer |

---

## UX Patterns

Standard patterns from weather map applications (Weather.com, Windy.com, Meteoblue, Ventusky).

### Marker Design

**Use custom `DivIcon` (HTML badge), NOT the default Leaflet blue pin.**

The default Leaflet pin is designed to mark a single point of interest. It cannot display dynamic text cleanly. The standard pattern for temperature maps is a **styled pill/badge**:

```
┌───────┐
│ 22°C  │   ← rounded pill, colored background, white/dark text
└───────┘
```

- Rounded pill (`border-radius: 9999px`) or card with slight shadow
- Background color derived from temperature range (see color coding below)
- Bold temperature number, smaller `°C` unit
- No arrow/anchor — the dot center IS the city location
- Optional: city name as `<Tooltip permanent>` below the badge at zoom ≥ 8

### Temperature Color Scale (Industry Standard)

| Range | Color | Semantic |
|-------|-------|---------|
| ≤ 0°C | `#3b82f6` (blue-500) | Freezing |
| 1–10°C | `#93c5fd` (blue-300) | Cold |
| 11–20°C | `#4ade80` (green-400) | Mild |
| 21–28°C | `#fb923c` (orange-400) | Warm |
| > 28°C | `#ef4444` (red-500) | Hot |

This is the universally understood weather color language. Using it requires zero user education.

### Loading State

- **Pattern**: Semi-transparent overlay on the map container with a centered spinner
- **NOT**: Blank page, hidden map, or placeholder boxes
- **Reason**: The map tiles load independently (Leaflet handles that); the overlay signals "temperature data incoming" without hiding the geographic context
- Show city marker positions immediately if coordinates are hardcoded (they are), fade in temperature values when data arrives

### Error State

- **Pattern**: Non-blocking banner at top of viewport ("Unable to load temperatures — retrying…") with a retry button
- **NOT**: Full-page error screen that hides the map
- **Reason**: Map tiles still render even if the API fails; showing the map with an error notice is more useful than a blank error page

### Map Tile Choice

- **OpenStreetMap default tiles**: Familiar, free, no API key — works; slightly busy visually
- **CartoDB Positron** (recommended for minimal design): Light grey, muted labels, white water — map content recedes so temperature badges pop. No API key required.
  - URL: `https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png`
- **NOT dark tiles**: User explicitly rejected dark/atmospheric theme

### Layout

```
┌─────────────────────────────────────────────────┐
│  🇧🇬 Bulgaria Weather          [last updated 14:00] │  ← minimal header, ~48px tall
├─────────────────────────────────────────────────┤
│                                                 │
│          [full-viewport Leaflet map]            │  ← calc(100vh - 48px)
│     with 28 temperature badge markers           │
│                                                 │
└─────────────────────────────────────────────────┘
```

- No sidebars, no bottom panels, no overlapping UI elements
- Header: app name left, last-updated right — both low-contrast so map stays hero
- Map: `width: 100%; height: calc(100vh - 48px)` — standard full-bleed map pattern

---

## Feature Dependencies

```
Custom DivIcon badge         → Temperature data fetched (API-01 + API-02)
Temperature color coding     → Custom DivIcon badge
City name tooltip            → Marker exists (MAP-02)
"Last updated" timestamp     → Data fetch complete
Auto-refresh                 → Data fetch hook (useWeatherData)
Loading overlay              → Data fetch hook (useWeatherData)
Error banner                 → Data fetch hook (useWeatherData)
```

All table stakes converge on one dependency: `GET /cities/temperatures` returning `[{ city, lat, lon, temperatureC }]`.

---

## MVP Recommendation

Given the user's explicit scope and the table stakes analysis:

**Build (non-negotiable):**
1. Leaflet map, CartoDB Positron tiles, centered on Bulgaria at zoom 7
2. 28 hardcoded city coordinates → marker positions
3. Custom `DivIcon` badge showing `temperatureC` fetched from `/cities/temperatures`
4. Loading overlay + error banner (UI-03)
5. City name via `<Tooltip>` (hover or permanent at high zoom) — prevents "which dot is which?" confusion

**Strongly recommended additions (LOW complexity, HIGH value):**
6. Temperature color coding — 10 lines of TypeScript, massive readability gain
7. "Last updated" timestamp — 3 lines, builds data trust

**Defer everything else.**

These 7 items constitute a complete, polished, production-ready v1 that exactly matches the user's stated intent.

---

## Sources

- Direct analysis of weather map UX conventions (Windy.com, Meteoblue, Ventusky, weather.com map view)
- Leaflet `DivIcon` documentation (standard approach for custom HTML markers)
- Open-Meteo API response schema (hourly `temperature_2m` for bulk coordinates)
- CartoDB basemaps (public, no API key): https://github.com/CartoDB/basemap-styles
- Project context: `.planning/PROJECT.md` — user's explicit exclusions honored throughout
