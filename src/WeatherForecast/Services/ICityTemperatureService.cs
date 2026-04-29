namespace WeatherForecast;

/// <summary>Provides current temperatures for all 28 Bulgarian district cities, with caching.</summary>
public interface ICityTemperatureService
{
    /// <summary>
    /// Returns current temperatures for all 28 Bulgarian district cities.
    /// Results are cached for 10 minutes (IMemoryCache, absolute TTL).
    /// </summary>
    /// <param name="cancellationToken">Propagated to the upstream Open-Meteo HTTP call on cache miss.</param>
    Task<IReadOnlyList<CityTemperature>> GetAllAsync(CancellationToken cancellationToken = default);
}
