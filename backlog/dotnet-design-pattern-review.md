# .NET Design Pattern Review — WeatherForecast API

**Date**: 2026-04-23  
**Scope**: `src/WeatherForecast/`

---

## Summary

| Area | Status | Priority |
|---|---|---|
| Service abstraction (SRP) | ❌ Missing | High |
| Repository/Provider pattern | ❌ Missing | Medium |
| Primary constructor syntax | ⚠️ Outdated | Low |
| Static config in controller | ⚠️ Code smell | Medium |
| Logger unused | ⚠️ Dead code | Low |
| Top-level Program.cs | ⚠️ Outdated | Low |
| Testability | ❌ Poor | High |
| Model immutability | ⚠️ Optional | Low |

---

## Findings

### 1. Missing Service Layer (No Separation of Concerns) — 🔴 High

Business logic (data generation, random selection) lives directly in the controller. This violates the **Single Responsibility Principle** and makes the controller untestable.

**Recommendation**: Extract to a `IWeatherForecastService` + `WeatherForecastService` pair:

```csharp
public interface IWeatherForecastService
{
    IEnumerable<WeatherForecast> GetForecasts(int count = 5);
}
```

Register as:
```csharp
services.AddScoped<IWeatherForecastService, WeatherForecastService>();
```

---

### 2. No Repository / Provider Pattern — 🟡 Medium

The data source (currently random, but could be a DB/API) is hardcoded in the controller. There is no abstraction for the data provider.

**Recommendation**: Introduce a `IWeatherDataProvider` interface so the underlying source (random, DB, external API) can be swapped without touching the controller.

---

### 3. Constructor Injection — Should Use Primary Constructor Syntax — 🟢 Low

The controller uses old-style constructor injection with a backing field. In C# 12 / .NET 8, primary constructors are preferred.

**Current**:
```csharp
private readonly ILogger<WeatherForecastController> _logger;

public WeatherForecastController(ILogger<WeatherForecastController> logger)
{
    _logger = logger;
}
```

**Recommended**:
```csharp
public class WeatherForecastController(ILogger<WeatherForecastController> logger) : ControllerBase
```

---

### 4. Static Data in Controller (`Summaries` array) — 🟡 Medium

`Summaries` is a `static readonly` field inside the controller — it is configuration/domain data that belongs elsewhere.

**Recommendation**: Move to a constants class or inject via `IOptions<T>` for configurability:

```csharp
public class WeatherForecastOptions
{
    public string[] Summaries { get; set; } = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];
}
```

---

### 5. Logger Injected but Never Used — 🟢 Low

`ILogger<WeatherForecastController>` is injected but never called — dead code.

**Recommendation**: Add structured logging at key points:
```csharp
logger.LogInformation("Returning {Count} forecasts", count);
```

---

### 6. `Program.cs` — Explicit Class/Main Instead of Top-Level Statements — 🟢 Low

`Program.cs` wraps everything in an explicit `Program` class with a `Main()` method. This is unnecessary in .NET 8 and adds boilerplate.

**Recommendation**: Use top-level statements (the .NET default), which also simplifies integration testing with `WebApplicationFactory<Program>`.

---

### 7. Poor Testability — 🔴 High

With no interfaces or abstractions around data generation, the controller cannot be unit tested without significant difficulty.

**Recommendation**: Once `IWeatherForecastService` exists, the controller becomes trivially testable. The service should depend on an injectable `TimeProvider` (built-in in .NET 8) and an abstracted randomness source for deterministic tests.

---

### 8. Model — Mutable Class Instead of Record — 🟢 Low

`WeatherForecast` is a plain mutable class with public setters. As a response-only DTO, immutability and value semantics are more appropriate.

**Recommendation**: Convert to a `record`:
```csharp
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```
