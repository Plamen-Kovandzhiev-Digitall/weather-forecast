# Stack Research — Bulgaria Weather Map

**Project:** Bulgaria Weather Temperature Map  
**Researched:** 2026-04-27  
**Overall confidence:** HIGH — all versions verified via npm registry and live API tests

---

## Existing Stack (Already In Place — Do Not Change)

The frontend at `src/weather-ui/` already exists with the following confirmed baseline:

| Layer | Technology | Version | Status |
|-------|-----------|---------|--------|
| Frontend runtime | React + React DOM | 19.1.0 | ✅ Installed |
| Build tool | Vite | 6.3.3 | ✅ Installed |
| Language | TypeScript | ~5.8.3 | ✅ Installed |
| Map library | react-leaflet | 5.0.0 | ✅ Installed |
| Map core | leaflet | 1.9.4 | ✅ Installed |
| Styling | Tailwind CSS | 3.4.17 | ✅ Installed (v3 LTS) |
| Backend | ASP.NET Core 8 | .NET 8 | ✅ Running on :5055 |

---

## Recommended Stack — New Additions

### Frontend

| Library | Version | Purpose | Rationale | Confidence |
|---------|---------|---------|-----------|------------|
| `react-leaflet` | **5.0.0** (already installed) | Map components | v5.0.0 is the stable `latest` on npm; its peer deps explicitly declare `react: "^19.0.0"` — **confirmed React 19 compatible**. No upgrade needed. | HIGH |
| `leaflet` | **1.9.4** (already installed) | Map engine | Required peer dep of react-leaflet v5. Already installed. `L.divIcon()` is the correct API for custom temperature badge markers. | HIGH |
| `@types/leaflet` | **1.9.14** (already installed) | TypeScript types for Leaflet | Without this, `L.divIcon`, `L.latLng`, etc. are untyped. Already installed. | HIGH |
| `tailwindcss` | **3.4.17 → upgrade to 4.2.4** | Utility CSS | v4.2.4 is now the stable `latest` (released 2025, active development). v3.4.x is `v3-lts`. For a new milestone, use v4: eliminates `postcss.config.js`, uses `@tailwindcss/vite` plugin (already in Vite config), CSS-first config replaces `tailwind.config.js`. See migration note below. | HIGH |
| `@tailwindcss/vite` | **4.2.4** | Tailwind v4 Vite integration | Replaces the PostCSS + `autoprefixer` approach. Add as Vite plugin; no separate PostCSS config needed. Faster than v3 PostCSS pipeline. | HIGH |

> **Tailwind v3 → v4 migration note:**  
> The project currently uses v3 (`tailwind.config.js` + `postcss.config.js`). Upgrading to v4 requires:
> 1. `npm install tailwindcss@latest @tailwindcss/vite` and remove `autoprefixer`, `postcss`
> 2. In `vite.config.ts`: add `import tailwindcss from '@tailwindcss/vite'` and add `tailwindcss()` to `plugins`
> 3. In `index.css`: replace `@tailwind base/components/utilities` with `@import "tailwindcss"`
> 4. Delete `tailwind.config.js` and `postcss.config.js`  
> 
> This is low-risk for a new feature milestone since the existing CSS is minimal. If migration risk is deemed unacceptable, **stay on v3.4.17** (fully functional, just the LTS path).

### Backend (Open-Meteo Integration)

| Library/API | Version | Purpose | Rationale | Confidence |
|-------------|---------|---------|-----------|------------|
| `IHttpClientFactory` (built-in) | .NET 8 | HTTP calls to Open-Meteo | Named client pattern: register `builder.Services.AddHttpClient("open-meteo", c => c.BaseAddress = new Uri("https://api.open-meteo.com/"))`. Avoids socket exhaustion vs `new HttpClient()`. Already available in ASP.NET Core 8, no new packages. | HIGH |
| `IMemoryCache` (built-in) | .NET 8 | Cache API responses | Open-Meteo updates temperatures every 15 minutes. Cache the 28-city response for 10–15 minutes using `builder.Services.AddMemoryCache()` — already in .NET 8 base library (`Microsoft.Extensions.Caching.Memory`). No new NuGet package. | HIGH |
| `System.Text.Json` (built-in) | .NET 8 | Deserialize Open-Meteo responses | Built-in, no Newtonsoft.Json needed. Use `JsonPropertyName` attributes or configure `JsonNamingPolicy.SnakeCaseLower` to match Open-Meteo's snake_case response format. | HIGH |
| `Swashbuckle.AspNetCore` | 6.6.2 (already installed) | Swagger UI for `/cities/temperatures` | Already in project. New endpoint auto-documented. No change needed. | HIGH |

---

## What NOT to Use

- **`OpenWeatherMap`** — requires API key management. Open-Meteo is free, no key, no rate limit concerns for 28 cities. Already confirmed working via live API test.
- **`axios` on frontend** — native `fetch` + `useEffect` hook pattern already works (see existing `useWeatherData.ts`). Adding axios is unnecessary complexity.
- **`react-query` / `@tanstack/query`** — overkill for a single endpoint fetched once on load. The existing hook pattern is sufficient. Would add ~13KB bundle weight.
- **`Newtonsoft.Json`** — `System.Text.Json` in .NET 8 handles snake_case via `JsonNamingPolicy.SnakeCaseLower`. No NuGet overhead.
- **`IDistributedCache` / Redis** — distributed caching is overkill for a single-process app serving 28 city temperatures. `IMemoryCache` is sufficient and zero-dependency.
- **`react-leaflet` < v5** — versions 4.x require React 16–18. Do not downgrade. v5.0.0 is explicitly built for React 19.
- **`tailwindcss@3` PostCSS pipeline** — if upgrading to v4, delete `postcss.config.js` and `autoprefixer`. The `@tailwindcss/vite` plugin handles everything. Running both simultaneously causes conflicts.
- **`@vitejs/plugin-react-swc`** — `@vitejs/plugin-react` (Babel, already installed) is the correct choice for this project size. SWC offers marginal build speed gains not worth a dependency swap.

---

## Key Integration Notes

### Open-Meteo Bulk Request Pattern

Open-Meteo supports fetching multiple coordinates in **one HTTP request** by passing comma-separated `latitude` and `longitude` arrays. Verified live (2026-04-27):

```
GET https://api.open-meteo.com/v1/forecast
  ?latitude=42.6977,43.2167,43.2048,...   ← up to 100 pairs
  &longitude=23.3219,27.9167,27.9167,...
  &current=temperature_2m
  &timezone=Europe/Sofia
```

**Response:** JSON array (one object per city), each with:
```json
{
  "latitude": 42.6875,
  "longitude": 23.3125,
  "location_id": 0,         ← index-based, use to correlate with input order
  "current": {
    "time": "2026-04-27T16:45",
    "interval": 900,         ← 15-minute update cadence
    "temperature_2m": 18.2   ← the value we need, in °C
  }
}
```

**Correlation strategy:** Response objects include `location_id` (0-indexed position in your input arrays). Use a hardcoded `CityCoordinates` list in the backend, ordered consistently. Map response by `location_id` index back to city name.

**Recommended backend endpoint shape:**
```
GET /cities/temperatures
Response: [{ "city": "София", "lat": 42.6977, "lon": 23.3219, "temperatureC": 18.2 }, ...]
```

**Caching approach:**
```csharp
// In controller or service — cache the entire 28-city array
var cacheKey = "cities_temperatures";
if (!_cache.TryGetValue(cacheKey, out IReadOnlyList<CityTemperature>? result))
{
    result = await FetchFromOpenMeteo();
    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
}
```

### react-leaflet Custom Temperature Marker (DivIcon)

For temperature badges directly on the map (not the default pin icon), use `L.divIcon`:

```tsx
import L from 'leaflet';

function createTempIcon(temp: number): L.DivIcon {
  return L.divIcon({
    className: '',  // ← clear Leaflet's default class so Tailwind controls styling
    html: `<div class="bg-white border border-gray-300 rounded px-1 text-sm font-semibold shadow">
             ${temp}°C
           </div>`,
    iconSize: [52, 28],
    iconAnchor: [26, 14],   // center the badge on the coordinate
  });
}

// In JSX:
<Marker position={[city.lat, city.lon]} icon={createTempIcon(city.temperatureC)} />
```

> **Note:** The `className: ''` override is critical. Without it, Leaflet applies `leaflet-div-icon` CSS which adds a white box with border that conflicts with Tailwind styling.

### Leaflet Default Icon Fix (already in codebase)

The existing `WeatherMap.tsx` already contains the standard Vite/bundler fix:
```ts
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({ iconUrl, iconRetinaUrl, shadowUrl });
```
This remains needed for any `Marker` using the default icon. For `DivIcon`-only markers, it's unnecessary but harmless.

### TypeScript Configuration

The existing `tsconfig.json` is well-configured for this stack:

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "moduleResolution": "bundler",
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noUncheckedSideEffectImports": true
  }
}
```

**No changes needed.** `moduleResolution: "bundler"` is correct for Vite 6. `strict: true` ensures safe null handling when Open-Meteo responses are parsed.

**New types to add** for the updated API (`src/types/weather.ts`):
```typescript
export interface CityTemperature {
  city: string;
  lat: number;
  lon: number;
  temperatureC: number;
}
```

### CORS

Already configured in `Program.cs` for `http://localhost:5173`. **No changes needed** — the new `/cities/temperatures` endpoint will inherit the same CORS policy.

---

## Installation Commands

```bash
# If upgrading to Tailwind v4 (recommended for new milestone):
cd src/weather-ui
npm install tailwindcss@latest @tailwindcss/vite@latest
npm uninstall autoprefixer postcss

# All other frontend deps already installed — no additional npm installs needed
```

```xml
<!-- Backend — no new NuGet packages needed -->
<!-- IHttpClientFactory, IMemoryCache, System.Text.Json all in .NET 8 base -->
```

---

## Sources

- react-leaflet v5 peer deps: `npm info react-leaflet peerDependencies` → `{ react: "^19.0.0" }` (verified 2026-04-27)
- react-leaflet dist-tags: `{ latest: "5.0.0", next: "5.0.0-rc.2" }` (verified 2026-04-27)
- tailwindcss dist-tags: `{ latest: "4.2.4", "v3-lts": "3.4.19" }` (verified 2026-04-27)
- @tailwindcss/vite dist-tags: `{ latest: "4.2.4" }` (verified 2026-04-27)
- Open-Meteo bulk API: live curl test with 3 Bulgarian cities (Sofia, Shumen, Shumen) — array response with `location_id` confirmed (2026-04-27)
- Open-Meteo docs: https://open-meteo.com/en/docs (no API key required, free tier, 15-min update interval)
- react-leaflet DivIcon API: https://react-leaflet.js.org/docs/api-components (Context7 verified)
- Existing project files: `src/weather-ui/package.json`, `tsconfig.json`, `vite.config.ts`, `src/WeatherForecast/Program.cs`
