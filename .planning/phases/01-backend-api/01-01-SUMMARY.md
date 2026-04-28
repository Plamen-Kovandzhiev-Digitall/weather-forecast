---
phase: 1
plan: 01-01
subsystem: data-foundation
tags: [data, models, dtos]
key-files:
  created:
    - src/WeatherForecast/Data/BulgarianCities.cs
    - src/WeatherForecast/Models/CityTemperature.cs
    - src/WeatherForecast/Models/OpenMeteoResponse.cs
---

# Plan 01-01 Summary: Foundation — Static City Data & DTOs

## What Was Built

Three foundational data files that all subsequent plans depend on:

1. **`Data/BulgarianCities.cs`** — Static `IReadOnlyList<CityInfo>` with exactly 28 Bulgarian district administrative centers. Each entry has `NameNative` (Cyrillic), `NameEn` (Latin transliteration), `Latitude`, and `Longitude`. Order is fixed and deterministic (Open-Meteo positional join depends on it).

2. **`Models/CityTemperature.cs`** — API response DTO with the D-07 locked shape: `{ NameNative, NameEn, Latitude, Longitude, TemperatureC }`. Used as the JSON response type for `GET /api/cities/temperatures`.

3. **`Models/OpenMeteoResponse.cs`** — Internal deserialization model for the Open-Meteo bulk array response. `Current` property is `OpenMeteoCurrent?` (nullable, per PITFALLS #10). Uses `[JsonPropertyName]` attributes for snake_case → PascalCase mapping.

## Commits

| Commit | Description |
|--------|-------------|
| 889c496 | feat(01-01): add BulgarianCities static list (28 cities) and DTOs |

## Deviations

None. Plan executed exactly as specified.

## Self-Check

- [x] 28 `CityInfo` entries in `BulgarianCities.All`
- [x] `OpenMeteoCurrent?` is nullable (PITFALLS #10 guard)
- [x] All files use `namespace WeatherForecast;` (file-scoped, flat)
- [x] `dotnet build` exits 0 with 0 errors
- [x] `CityTemperature` has exactly the 5 properties from D-07

**Self-Check: PASSED**
