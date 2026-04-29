namespace WeatherForecast;

using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Fetches current temperatures for all 28 Bulgarian district cities from Open-Meteo,
/// caching the result in IMemoryCache with a 10-minute absolute TTL (API-03).
/// Joins Open-Meteo response to BulgarianCities by positional index (PITFALLS #3).
/// </summary>
public class CityTemperatureService(
    IOpenMeteoClient openMeteoClient,
    IMemoryCache cache,
    ILogger<CityTemperatureService> logger) : ICityTemperatureService
{
    private const string CacheKey = "bg-city-temps";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CityTemperature>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<CityTemperature>? cached))
        {
            logger.LogDebug("Cache HIT for {CacheKey}", CacheKey);
            return cached!;
        }

        logger.LogInformation("Cache MISS for {CacheKey}. Fetching from Open-Meteo.", CacheKey);
        var cities = BulgarianCities.All;
        var responses = await openMeteoClient
            .GetCurrentTemperaturesAsync(cities, cancellationToken)
            .ConfigureAwait(false);

        // D-08: partial response guard — log warning but proceed with however many items arrived
        if (responses.Count < cities.Count)
            logger.LogWarning(
                "Open-Meteo returned {Actual} items; expected {Expected}. Proceeding with partial data.",
                responses.Count, cities.Count);

        // PITFALLS #3: join by positional index, NOT by coordinate matching
        var result = cities
            .Zip(responses, (city, r) => new CityTemperature
            {
                NameNative   = city.NameNative,
                NameEn       = city.NameEn,
                Latitude     = city.Latitude,
                Longitude    = city.Longitude,
                // PITFALLS #10: null-safe access — Current is nullable; null when Current is absent
                TemperatureC = r.Current?.Temperature2M,
            })
            .ToList()
            .AsReadOnly();

        // API-03: 10-minute absolute TTL
        cache.Set(CacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }
}
