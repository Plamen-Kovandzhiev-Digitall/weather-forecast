# Pitfalls Research — Bulgaria Weather Map

**Domain:** React SPA + ASP.NET Core proxy + Open-Meteo weather API  
**Researched:** 2026-04-27  
**Stack:** react-leaflet v5 · React 19 · Vite · TypeScript · Tailwind CSS · ASP.NET Core 8 · Open-Meteo

---

## Critical Pitfalls

These cause hard failures, data corruption, or silent wrong behaviour.

---

### 1. HttpClient Socket Exhaustion in the Proxy Controller

- **What goes wrong**: Creating `new HttpClient()` inside a controller constructor, controller action, or service constructor leads to socket exhaustion. Each `new HttpClient()` opens new TCP connections that linger in `TIME_WAIT`. Under light load on a dev machine it looks fine; under any repeated calls (polling, load tests, CI restarts) the OS runs out of sockets.
- **Warning signs**: `SocketException: Only one usage of each socket address is normally permitted` errors in the ASP.NET Core logs; intermittent 500s from the `/cities/temperatures` endpoint.
- **Prevention**: Register `HttpClient` via `IHttpClientFactory` in `Program.cs`:
  ```csharp
  builder.Services.AddHttpClient<IWeatherService, WeatherService>();
  // or named:
  builder.Services.AddHttpClient("open-meteo", c => {
      c.BaseAddress = new Uri("https://api.open-meteo.com/");
      c.Timeout = TimeSpan.FromSeconds(15);
  });
  ```
  Inject `IHttpClientFactory` (or the typed client) — never `new HttpClient()`.
- **Phase**: Backend API endpoint implementation (API-01 / API-02).

---

### 2. Wrong Open-Meteo Parameter: `current_weather` vs `current`

- **What goes wrong**: Open-Meteo has two generations of API. The old (pre-2023) parameter is `?current_weather=true`, which returns a `current_weather` object with a `temperature` field. The **current API** uses `?current=temperature_2m` and returns a `current` object with a `temperature_2m` field. Using the old parameter against the current API either returns no data or a deprecation-era fallback. Confirmed by live test: `current_weather=true` is silently absent from modern responses.
- **Warning signs**: Response JSON has no `current` key; temperatures are `null` or undefined in TypeScript; the map renders all markers with `NaN°C`.
- **Prevention**: Always use the modern form:
  ```
  GET https://api.open-meteo.com/v1/forecast
    ?latitude=...
    &longitude=...
    &current=temperature_2m
    &timezone=Europe/Sofia
  ```
  Deserialize `response.Current.Temperature2M` (C# naming) / `current.temperature_2m` (JSON).
- **Phase**: Backend API endpoint implementation (API-02).

---

### 3. Open-Meteo Bulk Response: Match by Array Index, Not by Coordinate

- **What goes wrong**: Open-Meteo's bulk request accepts comma-separated coordinates: `?latitude=42.5,43.2,43.8&longitude=27.47,27.92,25.97`. It returns a JSON **array** in the same order as the input coordinates — but it **snaps** each coordinate to the nearest ~7 km grid point. The returned `latitude`/`longitude` in each array element will NOT exactly match the input values. If you try to match a city name by looking up coordinates in the response, you'll get mismatches or fail to find cities.
- **Warning signs**: City temperatures are wrong (Varna shows Sofia's temperature); some cities show as "not found" despite valid coordinates; latitude mismatches of 0.0625–0.125°.
- **Prevention**: Map cities by **array index**, not by coordinate lookup:
  ```csharp
  var cities = BulgariaCities.All; // ordered list of 28 cities
  var temps  = openMeteoResponse; // array from Open-Meteo, same length
  var result = cities.Zip(temps, (city, temp) => new CityTemperature(
      city.Name, city.Lat, city.Lon,
      temp.Current.Temperature2M
  ));
  ```
  The `location_id` field in the response (0 for first, 1, 2, … for the rest) is just a sequence index — it confirms ordering but is not a city identifier.
- **Phase**: Backend API endpoint implementation (API-01 / API-02).

---

### 4. UseHttpsRedirection Breaks the HTTP Frontend Fetch

- **What goes wrong**: `Program.cs` currently has `app.UseHttpsRedirection()`. The Vite frontend fetches `http://localhost:5055/...` (plain HTTP). When the backend receives an HTTP request, `UseHttpsRedirection` issues a **307 redirect** to the HTTPS equivalent. This requires: (a) a trusted dev cert, and (b) HTTPS to be listening. The `http` launch profile in `launchSettings.json` only exposes `http://localhost:5055` — no HTTPS port is defined, so the redirect target becomes `https://localhost:443`, which is wrong. On a fresh machine without `dotnet dev-certs https --trust` the browser will reject the redirected HTTPS connection entirely, breaking all API calls silently (the error just says "Failed to fetch").
- **Warning signs**: Network tab shows the first fetch returns a 307, followed by a failed HTTPS request; console shows `net::ERR_CERT_AUTHORITY_INVALID` or `net::ERR_CONNECTION_REFUSED`.
- **Prevention — two options (pick one)**:
  1. **Remove `UseHttpsRedirection` for development** (simplest):
     ```csharp
     if (!app.Environment.IsDevelopment())
         app.UseHttpsRedirection();
     ```
  2. **Configure a Vite proxy** so the frontend never speaks directly to the backend:
     ```ts
     // vite.config.ts
     export default defineConfig({
       plugins: [react()],
       server: {
         proxy: {
           '/api': 'http://localhost:5055'
         }
       }
     })
     ```
     Then fetch `/api/cities/temperatures` — Vite forwards it. This also eliminates the CORS dependency entirely.
- **Phase**: Backend endpoint setup + Vite config (CORS-01).

---

### 5. Complete Frontend Data Model Overhaul Required

- **What goes wrong**: The existing frontend (`types/weather.ts`, `useWeatherData.ts`, `WeatherMap.tsx`, `WeatherPopup.tsx`, `App.tsx`) is built for the old `/weatherforecast` endpoint returning `{ date, temperatureC, temperatureF, summary }[]` for a single city (Veliko Tarnovo). The new endpoint returns temperatures for **28 cities**, with a completely different shape: `{ name, latitude, longitude, temperatureC }[]`. If any file is only partially updated, TypeScript will catch shape mismatches — but if types are loosened (`as any` or generic `object`) to silence errors, wrong data will silently pass through and the map will render incorrect markers.
- **Warning signs**: TypeScript errors in `WeatherMap.tsx` when passing new city data to old props; markers rendering with `undefined°C`; `WeatherPopup.tsx` referencing `f.summary` which no longer exists.
- **Prevention**: Treat this as a full replace, not a patch:
  - Delete `types/weather.ts` and rewrite with the new shape.
  - Replace `useWeatherData.ts` hook to call `/cities/temperatures`.
  - Rewrite `WeatherMap.tsx` to render 28 markers (not one).
  - Delete `WeatherPopup.tsx` entirely (it shows a 5-day forecast; the new app shows only current temperature per city).
  - Update `App.tsx` header copy (currently says "VELIKO TARNOVO"; should say "BULGARIA").
- **Phase**: Frontend map + marker implementation (MAP-01 through MAP-03, UI-01).

---

### 6. Custom DivIcon Must Be Memoized — Never Created Inline

- **What goes wrong**: MAP-03 requires temperature numbers displayed **directly on the map** as marker labels (e.g., a circle badge saying "14°C"), not the default pin icon. This requires `L.divIcon({ html: '...' })`. If the `divIcon` is created inside the render function body without `useMemo`, Leaflet receives a new icon object on every React state change (e.g., any parent re-render), treats it as a changed prop, and tears down and recreates all 28 markers simultaneously → visible flicker on every render cycle.
- **Warning signs**: Map markers flicker/disappear briefly when any state changes (including unrelated state like the loading badge); CPU spikes when hovering the map; React DevTools shows `Marker` components unmounting/remounting on every render.
- **Prevention**: Memoize per-city, keyed to temperature:
  ```tsx
  const icon = useMemo(() =>
    L.divIcon({
      className: '',
      html: `<div class="temp-badge">${Math.round(tempC)}°C</div>`,
      iconSize: [52, 28],
      iconAnchor: [26, 14],
    }),
    [tempC]  // recreate only when temperature changes
  );
  ```
  Put the memoized icon inside a `CityMarker` sub-component so each of the 28 markers manages its own memo independently.
- **Phase**: Frontend map marker implementation (MAP-03).

---

## Moderate Pitfalls

These cause degraded experience or subtle bugs if ignored.

---

### 7. MapContainer Props Are Immutable After Mount

- **What goes wrong**: react-leaflet's `MapContainer` docs explicitly state: *"Except for its children, MapContainer props are immutable — changing them after the initial render has no effect."* If the initial `center` or `zoom` is set incorrectly (e.g., still pointing at Veliko Tarnovo at zoom 12 instead of all-Bulgaria at zoom ~7), you cannot fix it by updating the prop. The map stays where it was initialized.
- **Warning signs**: Changing `center` or `zoom` in state has no visible effect; no console errors (it silently ignores the update).
- **Prevention**: Set `center` and `zoom` correctly from the start. Bulgaria centered: approximately `[42.7, 25.5]` at zoom `7`. If dynamic recentering is truly needed, use a `useMap()` child component to call `map.setView()` imperatively, or remount `MapContainer` via a React `key` prop.
- **Phase**: Frontend map setup (MAP-01).

---

### 8. CORS Policy Tied to a Single Port

- **What goes wrong**: The current CORS policy whitelists exactly `http://localhost:5173`. If a developer starts Vite on a different port (`vite --port 3000`, or port 5173 is in use), all fetch requests are blocked with `CORS error` — which looks identical to the backend being down. The error message in the browser gives no hint it's a port mismatch.
- **Warning signs**: `Access to fetch at 'http://localhost:5055/...' from origin 'http://localhost:3000' has been blocked by CORS policy`; backend logs show the request arriving but the browser discards the response.
- **Prevention**: Either use the Vite proxy approach (eliminates CORS entirely), or configure a broader dev CORS policy:
  ```csharp
  // Development only — never use in production
  if (app.Environment.IsDevelopment())
  {
      options.AddPolicy("AllowReactDev", policy =>
          policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod());
  }
  ```
- **Phase**: Backend CORS setup (CORS-01).

---

### 9. No Explicit HttpClient Timeout → Hanging Requests

- **What goes wrong**: The default `HttpClient.Timeout` is **100 seconds**. If Open-Meteo is unreachable or very slow, the frontend will show its loading spinner for up to 100 seconds before the proxy times out and returns a 500. Users will think the app is broken.
- **Warning signs**: Loading state persists for much longer than expected; no error appears for ~2 minutes.
- **Prevention**: Set an explicit short timeout when registering the client:
  ```csharp
  builder.Services.AddHttpClient("open-meteo", c => {
      c.BaseAddress = new Uri("https://api.open-meteo.com/");
      c.Timeout = TimeSpan.FromSeconds(10);
  });
  ```
  Also add a `CancellationToken` parameter to the controller action so ASP.NET Core can cancel the downstream request when the browser navigates away.
- **Phase**: Backend API endpoint implementation (API-02).

---

### 10. Nullable Reference Types: Open-Meteo Response Deserialization

- **What goes wrong**: The .NET project has nullable reference types enabled (`<Nullable>enable</Nullable>`). When deserializing the Open-Meteo response with `System.Text.Json`, all reference-type fields default to `null` if absent. If `current` or `current.temperature_2m` is missing (e.g., Open-Meteo returns an error object instead of a location object), accessing `.Current.Temperature2M` throws a `NullReferenceException` that surfaces as an unhandled 500.
- **Warning signs**: 500 errors from the backend when Open-Meteo returns a partial or error response; `CS8602: Dereference of a possibly null reference` warnings if warnings-as-errors is enabled.
- **Prevention**: Model the DTO with nullable properties and null-check before mapping:
  ```csharp
  public record OpenMeteoLocation(
      double Latitude,
      double Longitude,
      OpenMeteoCurrent? Current  // nullable
  );
  // Then:
  var temp = location.Current?.Temperature2M ?? double.NaN;
  ```
- **Phase**: Backend API endpoint implementation (API-02).

---

## Minor Pitfalls

Low impact, but easy to fix once known.

---

### 11. Hardcoded API URL With Wrong Endpoint Path

- **What goes wrong**: `useWeatherData.ts` hardcodes `http://localhost:5055/weatherforecast`. The new endpoint is `/cities/temperatures`. If a developer only adds the new controller without updating the hook, the frontend keeps calling the old random-data endpoint and shows no city markers. No error is thrown — the old endpoint still returns HTTP 200 with random forecast data.
- **Prevention**: Update the hook URL to `/cities/temperatures`. If using Vite proxy, use a relative URL: `fetch('/api/cities/temperatures')`.
- **Phase**: Frontend hook refactor (UI-01).

---

### 12. `WeatherPopup.tsx` References Obsolete Data Shape

- **What goes wrong**: `WeatherPopup.tsx` renders a 5-day forecast table with `SUMMARY_ICONS` keyed on strings like `"Warm"`, `"Chilly"`, etc. The new API has no summary field. This component is entirely incompatible with the new data model and must be deleted, not patched.
- **Prevention**: Delete `WeatherPopup.tsx` during the refactor. The new design shows temperature directly on the marker; popups (if added later) would show city name + temperature only.
- **Phase**: Frontend refactor (UI-01, MAP-03).

---

### 13. Open-Meteo Returns Coordinates Snapped to Grid — Don't Use for Marker Positioning

- **What goes wrong**: The `latitude`/`longitude` fields in each Open-Meteo response item are the **snapped** grid coordinates, not the exact city coordinates you sent. Extracting these to use as the Leaflet `position` prop for markers will place all markers slightly off from the actual city locations (by up to ~7 km). For a country-level zoom (zoom 7), this is visually acceptable, but for higher zoom levels the mismatch becomes obvious.
- **Prevention**: Always use the city's **canonical coordinates** from your own city list (hardcoded or from a config file) for marker positioning. Only use the Open-Meteo response for the temperature value.
- **Phase**: Frontend marker implementation (MAP-02).

---

### 14. App Header Still References "Veliko Tarnovo"

- **What goes wrong**: `App.tsx` has hardcoded `<h1>VELIKO TARNOVO</h1>` and `<div>Bulgaria · 43°N 25°E</div>`. After migrating to the 28-city national map, this header becomes inaccurate. It's a cosmetic issue but creates a confusing UX (header says one city, map shows all of Bulgaria).
- **Prevention**: Update header to "BULGARIA" or "BULGARIA WEATHER" during the frontend refactor.
- **Phase**: Frontend UI cleanup (UI-01, UI-02).

---

### 15. `start.ps1` May Not Start Both Backend and Frontend

- **What goes wrong**: The repo has a `start.ps1` script. If it only starts the backend (or only the frontend), developers won't realize both processes need to run simultaneously. There's no error — the app simply shows perpetual "API OFFLINE" state.
- **Warning signs**: The app shows the error badge immediately; network tab shows `ERR_CONNECTION_REFUSED` to the backend port.
- **Prevention**: Verify `start.ps1` starts both `dotnet run` (backend) and `npm run dev` (frontend) in parallel. Document this in the README.
- **Phase**: Dev setup / documentation.

---

## Phase-Specific Warning Table

| Phase / Topic | Likely Pitfall | Mitigation |
|---|---|---|
| Backend: Open-Meteo integration | Wrong `current_weather` parameter | Use `current=temperature_2m`; verify with live test |
| Backend: Open-Meteo integration | Socket exhaustion from `new HttpClient()` | `IHttpClientFactory` + typed client |
| Backend: Open-Meteo integration | City↔temperature index mismatch | Zip city list with response array by index |
| Backend: Open-Meteo integration | Null response deserialization crash | Nullable DTO properties + null-check before mapping |
| Backend: CORS / HTTPS | HTTP→HTTPS redirect breaks frontend | Disable `UseHttpsRedirection` in dev OR use Vite proxy |
| Frontend: Map setup | Wrong initial center/zoom (immutable after mount) | Set `center={[42.7, 25.5]}` zoom `7` from the start |
| Frontend: Markers | DivIcon flicker from inline creation | `useMemo` in per-city `CityMarker` component |
| Frontend: Markers | Marker positions use snapped coordinates | Use canonical city coords for positioning |
| Frontend: Data model | Old type shape causes silent wrong data | Full delete-and-rewrite of types + components |
| Frontend: URL | Old endpoint URL still in hook | Update hook to `/cities/temperatures` |

---

## Sources

- Open-Meteo live API test (2026-04-27): confirmed `current=temperature_2m` response shape and array-indexed bulk response
- react-leaflet official docs (Context7, `https://react-leaflet.js.org/docs/api-components`): MapContainer immutability confirmed
- `src/WeatherForecast/Program.cs` code review: `UseHttpsRedirection` + `UseCors("AllowReactDev")` for `http://localhost:5173` only
- `src/weather-ui/src/hooks/useWeatherData.ts`: hardcoded `http://localhost:5055/weatherforecast`
- `src/weather-ui/vite.config.ts`: no proxy configured
- `src/WeatherForecast/Properties/launchSettings.json`: `http` profile is HTTP-only on port 5055
- ASP.NET Core documentation: `IHttpClientFactory` guidance for avoiding socket exhaustion (HIGH confidence)
