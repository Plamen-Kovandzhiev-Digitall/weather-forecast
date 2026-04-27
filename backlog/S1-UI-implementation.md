# S1 — React Weather Forecast UI

**Status**: Ready  
**Assigned agent**: `Expert React Frontend Engineer`  
**Date**: 2026-04-27

---

## Objective

Create a React-based single-page application that displays a map centered on Veliko Tarnovo, Bulgaria. The weather forecast data retrieved from the existing ASP.NET Core API (`GET /WeatherForecast`) should be displayed on the map as forecast information for Veliko Tarnovo.

---

## Technical Specification

### Project Setup

- **Location**: `src/weather-ui/` (separate from the ASP.NET backend)
- **Tooling**: Vite + React 19 + TypeScript
- **Styling**: Tailwind CSS
- **Map library**: Leaflet via `react-leaflet`
- **Key dependencies**: `react`, `react-dom`, `react-leaflet`, `leaflet`, `@types/leaflet`, `tailwindcss`

### API Integration

- **Endpoint**: `GET http://localhost:5055/weatherforecast`
- **Response schema** (array of 5 items):

```json
[
  {
    "date": "2026-04-28",
    "temperatureC": 22,
    "temperatureF": 71,
    "summary": "Warm"
  }
]
```

- Fetch data on component mount using `fetch` or a lightweight hook.
- Handle loading and error states gracefully.

### Map Requirements

- **Library**: Leaflet with `react-leaflet` wrapper
- **Center**: Veliko Tarnovo, Bulgaria — coordinates `43.0757, 25.6172`
- **Zoom level**: ~12 (city-level view)
- **Tile provider**: OpenStreetMap (`https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png`)
- **Marker**: A single marker placed at the Veliko Tarnovo coordinates
- **Popup**: Clicking the marker opens a popup displaying the 5-day weather forecast

### Popup Content

The popup should show all 5 forecast entries with:

- **Date** (formatted, e.g. "Mon, Apr 28")
- **Temperature** in both °C and °F
- **Summary** (e.g. "Warm", "Chilly")

The popup should be visually polished — not a plain text dump. Use styled cards or a compact table layout inside the popup.

### UI / Design Direction

Follow the `frontend-design` skill guidelines (`.github/skills/frontend-design/SKILL.md`):

- Choose a **distinctive aesthetic** — avoid generic AI-generated look (no Inter/Roboto, no purple-on-white gradients).
- Use a **bold typographic** choice for the page title and forecast data.
- The map should be the **hero element**, taking most of the viewport.
- Add a **header/title bar** with the app name (e.g. "Veliko Tarnovo Weather") styled with character.
- Consider a **dark or atmospheric theme** fitting for a weather application.
- Add subtle **animations** for data loading transitions.

### CORS Configuration (Backend Change)

Add a CORS policy to `src/WeatherForecast/Program.cs` to allow requests from the Vite dev server:

```csharp
// After var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// After var app = builder.Build();
app.UseCors("AllowReactDev");
```

This should be added **before** `app.MapControllers()`.

---

## File Structure (Expected Output)

```
src/weather-ui/
├── index.html
├── package.json
├── tsconfig.json
├── vite.config.ts
├── tailwind.config.js
├── postcss.config.js
├── public/
│   └── (favicon or assets if needed)
├── src/
│   ├── main.tsx
│   ├── App.tsx
│   ├── App.css (or index.css for Tailwind base)
│   ├── components/
│   │   ├── WeatherMap.tsx        # Map with marker and popup
│   │   └── WeatherPopup.tsx      # Popup content for the marker
│   ├── hooks/
│   │   └── useWeatherData.ts     # Custom hook to fetch forecast data
│   └── types/
│       └── weather.ts            # TypeScript interfaces for API response
```

---

## Acceptance Criteria

1. Running `npm install && npm run dev` in `src/weather-ui/` starts the app on `http://localhost:5173`.
2. The app displays an interactive Leaflet map centered on Veliko Tarnovo.
3. A marker is visible at the Veliko Tarnovo coordinates.
4. Clicking the marker opens a popup with the 5-day weather forecast.
5. Forecast data is fetched from `GET http://localhost:5055/weatherforecast`.
6. Loading and error states are handled (spinner/message while loading, error message on failure).
7. The CORS policy is added to `src/WeatherForecast/Program.cs`.
8. The UI has a distinctive, polished design — not a generic template look.
9. All code is TypeScript with no `any` types.
10. The project builds without errors (`npm run build`).

---

## Reference Files

| File                                                           | Purpose                                                                  |
| -------------------------------------------------------------- | ------------------------------------------------------------------------ |
| `src/WeatherForecast/Controllers/WeatherForecastController.cs` | API endpoint returning 5 random forecasts                                |
| `src/WeatherForecast/WeatherForecast.cs`                       | Data model: `Date`, `TemperatureC`, `TemperatureF` (computed), `Summary` |
| `src/WeatherForecast/Program.cs`                               | Add CORS policy here                                                     |
| `src/WeatherForecast/Properties/launchSettings.json`           | API runs on `http://localhost:5055`                                      |
| `.github/skills/frontend-design/SKILL.md`                      | Design quality guidelines                                                |
