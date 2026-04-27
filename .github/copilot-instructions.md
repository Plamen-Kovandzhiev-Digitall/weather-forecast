# Copilot Instructions

## Project Overview

ASP.NET Core 8 Web API serving random weather forecast data. Single project, no test projects currently exist.

## Architecture

```
src/WeatherForecast/
├── Program.cs                    # App entry point; registers services, Swagger, controllers
├── WeatherForecast.cs            # Model: Date, TemperatureC, TemperatureF (computed), Summary
├── Controllers/
│   └── WeatherForecastController.cs  # GET /weatherforecast — returns 5 random forecasts
├── Dockerfile                    # Multi-stage Linux build; exposes 8080/8081
└── WeatherForecast.sln
```

- `TemperatureF` is a computed property (`32 + (int)(TemperatureC / 0.5556)`) — do not store it separately.
- Controllers use `[Route("[controller]")]` convention-based routing.
- Swagger/OpenAPI is enabled in Development only.

## Build & Run

```bash
# From project directory
cd src/WeatherForecast

# Restore & build
dotnet restore
dotnet build

# Run (http: localhost:5055, swagger at /swagger)
dotnet run --launch-profile http

# Docker
docker build -t weather-forecast .
docker run -p 8080:8080 weather-forecast
```

## Key Conventions

- **Namespace**: `WeatherForecast` (matches project name) — no sub-namespaces currently used.
- **Nullable reference types** are enabled (`<Nullable>enable</Nullable>`).
- **Implicit usings** are enabled — no need to add `using System;` etc. explicitly.
- Target framework: **net8.0**.

## Documentation

- All documents (stories, plans, reviews, backlog items, comments) must be written in **English**.
