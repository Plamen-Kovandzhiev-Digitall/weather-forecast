# Architecture Research — Bulgaria Weather Map

**Project:** Weather Forecast — Bulgaria Temperature Map  
**Researched:** 2026-04-27  
**Confidence:** HIGH (Open-Meteo bulk response verified via live API call)

---

## System Architecture

```
Browser
  │
  │  GET /api/cities/temperatures
  ▼
┌─────────────────────────────────────────────────────┐
│  ASP.NET Core 8  (localhost:5055)                   │
│                                                     │
│  CitiesController                                   │
│       │                                             │
│       ▼                                             │
│  CityTemperatureService                             │
│       │          │                                  │
│       │    IMemoryCache ◄─── cache hit (10 min TTL) │
│       │    (miss)                                   │
│       ▼                                             │
│  OpenMeteoClient  (IHttpClientFactory)              │
│       │                                             │
└───────┼─────────────────────────────────────────────┘
        │  Single GET request
        │  ?latitude=42.69,42.50,...  (28 values)
        │  &longitude=23.32,27.46,...  (28 values)
        │  &current=temperature_2m
        ▼
  api.open-meteo.com/v1/forecast
        │
        │  JSON array[28] — one object per city
        ▼
┌───────────────────────────────────┐
│  [{ latitude, longitude,          │
│     current: { temperature_2m }}  │
│   , ... × 28 ]                   │
└───────────────────────────────────┘
        │
        │  (join by index with BulgarianCities list)
        ▼
  CityTemperature[] →  cache → JSON response
        │
        ▼
┌─────────────────────────────────────────────────────┐
│  React 19 SPA  (localhost:5173)                     │
│                                                     │
│  App.tsx                                            │
│   └── useCityTemperatures (hook)                    │
│   └── WeatherMap.tsx                                │
│        ├── MapContainer (react-leaflet)             │
│        │    center: Bulgaria [42.7, 25.5], zoom: 7  │
│        └── TemperatureMarker × 28                   │
│              DivIcon showing "18°C" at each city    │
└─────────────────────────────────────────────────────┘
```

---

## Components

### Backend (ASP.NET Core)

| Component | File | Responsibility |
|-----------|------|----------------|
| `CitiesController` | `Controllers/CitiesController.cs` | Exposes `GET /api/cities/temperatures`; delegates to service; returns `CityTemperature[]` JSON |
| `CityTemperatureService` | `Services/CityTemperatureService.cs` | Checks `IMemoryCache`; on miss calls `OpenMeteoClient`; joins result with `BulgarianCities`; writes cache |
| `OpenMeteoClient` | `Services/OpenMeteoClient.cs` | Wraps `HttpClient` (via `IHttpClientFactory`); builds single bulk URL; deserializes array response |
| `BulgarianCities` | `Data/BulgarianCities.cs` | Static readonly list of 28 `{ Name, Latitude, Longitude }` records (coordinates hardcoded — they never change) |
| `CityTemperature` (DTO) | `Models/CityTemperature.cs` | Response model: `{ string Name, double Latitude, double Longitude, double TemperatureC }` |
| `OpenMeteoResponse` (internal) | `Models/OpenMeteoResponse.cs` | Deserialization target for Open-Meteo array elements: `{ double Latitude, double Longitude, OpenMeteoCurrent Current }` |

### Frontend (React)

| Component | File | Responsibility |
|-----------|------|----------------|
| `App.tsx` | `src/App.tsx` | Root component; passes data from hook to `WeatherMap`; shows header with "Bulgaria Live Temperatures" |
| `useCityTemperatures` | `src/hooks/useCityTemperatures.ts` | `fetch` hook hitting `GET /api/cities/temperatures`; returns `{ data, loading, error }` |
| `WeatherMap` | `src/components/WeatherMap.tsx` | `MapContainer` centered on Bulgaria `[42.7, 25.5]` zoom 7; renders 28 `TemperatureMarker` instances |
| `TemperatureMarker` | `src/components/TemperatureMarker.tsx` | Single city marker using Leaflet `DivIcon`; displays temperature text (e.g. `"18°C"`) directly on map; color-coded by temperature range |
| `CityTemperature` (type) | `src/types/cityTemperature.ts` | TypeScript interface matching backend DTO |

---

## Data Flow

```
1. User opens http://localhost:5173
2. App.tsx renders → useCityTemperatures() fires
3. fetch('http://localhost:5055/api/cities/temperatures')
4. [loading=true] → WeatherMap shows skeleton/spinner overlay

5. ASP.NET Core receives GET /api/cities/temperatures
6. CitiesController → CityTemperatureService.GetAllAsync()
7. IMemoryCache.TryGetValue("bg-city-temps") → MISS (first request)
8. CityTemperatureService reads BulgarianCities.All (28 cities)
9. OpenMeteoClient builds URL:
     https://api.open-meteo.com/v1/forecast
       ?latitude=42.6977,42.5018,43.2141,...  (28 values)
       &longitude=23.3219,27.4626,27.9147,...  (28 values)
       &current=temperature_2m
       &forecast_days=1
10. HttpClient sends ONE request to Open-Meteo
11. Open-Meteo returns JSON array[28], each element: 
      { latitude, longitude, current: { time, temperature_2m } }
12. OpenMeteoClient deserializes array
13. CityTemperatureService zips by index:
      BulgarianCities.All[i] + openMeteoArray[i].Current.Temperature2m
      → CityTemperature { Name, Latitude, Longitude, TemperatureC }
14. Result stored in IMemoryCache with 10-minute absolute expiry
15. CitiesController returns JSON array[28]

16. React receives response → [loading=false, data=CityTemperature[28]]
17. WeatherMap renders MapContainer (Bulgaria-wide view)
18. For each city: TemperatureMarker renders DivIcon at [lat, lng]
    → styled div: "Sofia\n18°C" — color blue (<0°), green (0-15°),
      orange (15-25°), red (>25°)
19. User sees 28 labeled temperature dots on Bulgaria map
```

**On subsequent requests (within 10 minutes):**
Steps 7 → cache HIT → skips steps 8-14, returns cached array directly.

---

## Key Architecture Decisions

| Decision | Options | Recommendation | Rationale |
|----------|---------|----------------|-----------|
| **Open-Meteo call strategy** | A) 28 individual requests  B) 1 bulk request | **B — single bulk request** | Open-Meteo accepts comma-separated `latitude` & `longitude` for up to 100 locations; returns positionally-ordered array. Verified live: 3 cities → `[{…},{…},{…}]`. One network round-trip vs 28. |
| **Response caching** | A) No cache  B) `IMemoryCache` (in-process)  C) `IDistributedCache` (Redis) | **B — IMemoryCache, 10 min TTL** | Single-instance API; no Redis needed. Open-Meteo refreshes every 15 min (`"interval": 900`), so 10 min captures fresh data while protecting against hammering. Built into ASP.NET Core, zero extra dependencies. |
| **City coordinates storage** | A) Hardcoded in backend C# class  B) `appsettings.json`  C) Database  D) Frontend TypeScript | **A — static C# class `BulgarianCities`** | Coordinates are geographic constants — they never change. No runtime config needed. Backend is the proxy; frontend receives `(lat, lng)` in the response and never needs to maintain a parallel list. Keeps frontend thin. |
| **Coordinate delivery to frontend** | A) Frontend hardcodes coordinates  B) Backend returns `(lat, lng)` in each response item | **B — backend returns `(lat, lng)` per city** | Single source of truth for coordinates. Frontend only places markers at positions given by the API. No duplication, no sync risk. |
| **Temperature marker style** | A) Default Leaflet marker + popup  B) Custom `DivIcon` with temperature text | **B — DivIcon** | Goal is "temperature visible directly on map" without clicking. `DivIcon` renders arbitrary HTML at map coordinates. Colored by temperature range for at-a-glance reading. |
| **Cache TTL value** | 5 min / 10 min / 15 min / 30 min | **10 minutes** | Open-Meteo data updates every 15 min. A 10-min cache ensures data is never more than 25 min stale while giving meaningful hit rate under normal usage. Avoids hitting Open-Meteo rate limits. |
| **HttpClient management** | `new HttpClient()` / `IHttpClientFactory` | **IHttpClientFactory** | Prevents socket exhaustion. Registered as named client `"open-meteo"` in `Program.cs`. Standard ASP.NET Core practice. |

---

## TypeScript Types (Frontend)

```typescript
// src/types/cityTemperature.ts

export interface CityTemperature {
  name: string;        // "София", "Пловдив", etc.
  latitude: number;    // WGS84
  longitude: number;   // WGS84
  temperatureC: number; // e.g. 18.2
}

export interface CityTemperaturesResponse {
  cities: CityTemperature[];
  fetchedAt: string; // ISO 8601 — returned by backend for display
}
```

> **Note:** Backend response is a flat `CityTemperature[]` array (not wrapped). The frontend
> can derive `fetchedAt` from a response header or a wrapper object if desired.
> Keep it a flat array for simplicity — matches how `useCityTemperatures` consumes it.

---

## Backend Response Schema

```json
[
  {
    "name": "София",
    "latitude": 42.6977,
    "longitude": 23.3219,
    "temperatureC": 18.2
  },
  {
    "name": "Пловдив",
    "latitude": 42.1500,
    "longitude": 24.7500,
    "temperatureC": 20.1
  },
  ...
]
```

28 items, ordered alphabetically or by the `BulgarianCities.All` list order.

---

## ASP.NET Core Registration (Program.cs additions)

```csharp
// Named HttpClient for Open-Meteo
builder.Services.AddHttpClient("open-meteo", client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// In-process cache (no extra packages)
builder.Services.AddMemoryCache();

// Service registrations
builder.Services.AddScoped<ICityTemperatureService, CityTemperatureService>();
builder.Services.AddScoped<IOpenMeteoClient, OpenMeteoClient>();
```

---

## Build Order

Dependencies flow from data → service → API → UI:

| Step | Component | Depends On | Notes |
|------|-----------|-----------|-------|
| 1 | `BulgarianCities.cs` — static city list | nothing | 28 cities with name + WGS84 coordinates; the foundation everything else uses |
| 2 | `CityTemperature.cs` — backend DTO model | nothing | Shared shape between service and controller |
| 3 | `OpenMeteoResponse.cs` — deserialization model | nothing | Internal model; only `OpenMeteoClient` uses it |
| 4 | `OpenMeteoClient` | HttpClient, models | Core external integration; test this in isolation against live API |
| 5 | `CityTemperatureService` | `OpenMeteoClient`, `BulgarianCities`, `IMemoryCache` | Orchestrates fetch + cache + join |
| 6 | `CitiesController` | `CityTemperatureService` | Thin controller; just delegates and returns |
| 7 | `Program.cs` additions | All above | Wire up DI: `AddHttpClient`, `AddMemoryCache`, service registrations |
| 8 | `src/types/cityTemperature.ts` | Backend contract (step 2) | Define TS interface matching backend DTO |
| 9 | `useCityTemperatures.ts` | Types (step 8), backend running (step 7) | Hook: fetch → parse → `{ data, loading, error }` |
| 10 | `TemperatureMarker.tsx` | Types (step 8) | Custom DivIcon component; can be built and previewed independently |
| 11 | `WeatherMap.tsx` (refactor) | `TemperatureMarker` (step 10), hook (step 9) | Replace single-city map with Bulgaria-wide 28-marker map |
| 12 | `App.tsx` (refactor) | `WeatherMap` (step 11), hook (step 9) | Update title, wiring |

**Critical path:** Steps 1–7 (backend) must be complete before step 9 (frontend hook) can be
validated end-to-end. Steps 8 and 10 can be developed in parallel with the backend.

---

## What the Existing Code Already Provides

| Already Done | Status |
|-------------|--------|
| CORS policy (`AllowReactDev` for `localhost:5173`) | ✅ `Program.cs` |
| Controller pattern with `[ApiController]` + DI logger | ✅ `WeatherForecastController.cs` |
| React 19 + Vite + TypeScript + Tailwind + react-leaflet | ✅ `weather-ui/package.json` |
| `useWeatherData` hook pattern (fetch + loading + error) | ✅ needs cloning for new endpoint |
| `WeatherMap` + `MapContainer` wiring | ✅ needs center/zoom change + marker swap |
| `types/weather.ts` TypeScript interface pattern | ✅ model for new `cityTemperature.ts` |

**What the existing frontend is NOT:** The current `App.tsx` / `WeatherMap.tsx` shows a
single-city 5-day forecast popup at Veliko Tarnovo (S1 milestone). This milestone replaces
that view with a Bulgaria-wide current-temperature map. The two are **incompatible** at the
top level — `App.tsx` and `WeatherMap.tsx` will be substantially rewritten. The hook and
types patterns are reused as templates.

---

## Pitfalls in This Architecture

| Risk | Mitigation |
|------|-----------|
| Open-Meteo array ordering — response is positional | Always build URL by iterating `BulgarianCities.All` in a stable, deterministic order; join by index, not by lat/lng matching (floating-point comparison is fragile) |
| `location_id` field in Open-Meteo response | First city has no `location_id`, subsequent cities have `location_id: 1, 2, ...`. Do not use it for joining — always use positional index |
| `IMemoryCache` lost on app restart | Acceptable for this use case (first request after restart makes one fresh Open-Meteo call) |
| Leaflet DivIcon CSS isolation | DivIcon HTML renders outside React's shadow; use `!important` or dedicated CSS classes. Avoid Tailwind utility classes directly in `divIcon({ html: '...' })` — they may not be present at icon render time |
| Frontend calls backend directly from browser | CORS already configured; ensure Vite proxy is NOT used (direct localhost:5055 call is correct for this project) |

---

## Sources

- Open-Meteo multi-location response format: **verified live** via `curl` (2026-04-27)  
  `GET /v1/forecast?latitude=42.69,42.50,43.0&longitude=23.32,27.46,25.62&current=temperature_2m`  
  → Returns `JSON array[3]` with `current.temperature_2m` per element (**HIGH confidence**)
- Open-Meteo docs `/v1/forecast`: Context7 `/websites/open-meteo_en` — "latitude (float), Can be comma-separated for multiple locations" (**HIGH confidence**)
- ASP.NET Core `IMemoryCache`: built-in `Microsoft.Extensions.Caching.Memory` (**HIGH confidence**)
- `IHttpClientFactory` pattern: standard ASP.NET Core since .NET Core 2.1 (**HIGH confidence**)
- Open-Meteo data refresh interval: `"interval": 900` (15 min) confirmed in live API response (**HIGH confidence**)
