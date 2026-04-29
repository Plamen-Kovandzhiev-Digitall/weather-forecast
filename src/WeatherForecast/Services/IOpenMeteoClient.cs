namespace WeatherForecast;

/// <summary>Abstracts the Open-Meteo bulk temperature fetch for testability and DI.</summary>
public interface IOpenMeteoClient
{
    /// <summary>
    /// Fetches current temperatures for all provided cities in a single bulk HTTP request.
    /// Returns responses in the same positional order as <paramref name="cities"/>.
    /// Retries up to 3 times with exponential backoff before propagating exceptions.
    /// </summary>
    /// <param name="cities">Ordered list of cities. Order MUST be stable — response joined by index.</param>
    /// <param name="cancellationToken">Allows ASP.NET Core to cancel if browser navigates away.</param>
    /// <returns>Positionally-ordered Open-Meteo response array (same length as input on success).</returns>
    Task<IReadOnlyList<OpenMeteoResponse>> GetCurrentTemperaturesAsync(
        IReadOnlyList<CityInfo> cities,
        CancellationToken cancellationToken = default);
}
