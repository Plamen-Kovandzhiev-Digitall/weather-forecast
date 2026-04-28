namespace WeatherForecast;

/// <summary>
/// Wraps Open-Meteo's bulk forecast API endpoint.
/// Sends a single HTTP request for all 28 cities using comma-separated coordinate lists.
/// Implements 3-attempt retry with exponential backoff (100ms → 300ms) per D-01.
/// </summary>
public class OpenMeteoClient(
    IHttpClientFactory httpClientFactory,
    ILogger<OpenMeteoClient> logger) : IOpenMeteoClient
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<OpenMeteoResponse>> GetCurrentTemperaturesAsync(
        IReadOnlyList<CityInfo> cities,
        CancellationToken cancellationToken = default)
    {
        // Build single bulk URL — comma-separated lat/lon in same order as cities list
        var lats = string.Join(",", cities.Select(c => c.Latitude));
        var lons = string.Join(",", cities.Select(c => c.Longitude));
        // PITFALLS #2: use current=temperature_2m NOT current_weather=true
        var url = $"/v1/forecast?latitude={lats}&longitude={lons}&current=temperature_2m&forecast_days=1";

        // Attempts 1 and 2 — retry with exponential backoff on failure (D-01)
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                // PITFALLS #1: never new HttpClient() — always use factory
                var client = httpClientFactory.CreateClient("open-meteo");
                var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var result = await response.Content
                    .ReadFromJsonAsync<IReadOnlyList<OpenMeteoResponse>>(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return result ?? [];
            }
            catch (Exception ex)
            {
                // D-03: log at each retry attempt
                var delayMs = (int)(100 * Math.Pow(3, attempt - 1));
                logger.LogWarning(ex,
                    "Open-Meteo attempt {Attempt} of 3 failed. Retrying in {DelayMs}ms.",
                    attempt, delayMs);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // Attempt 3 — final attempt, no catch: exception propagates to CityTemperatureService
        // then to CitiesController which returns HTTP 503 (D-02)
        var finalClient = httpClientFactory.CreateClient("open-meteo");
        var finalResponse = await finalClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        finalResponse.EnsureSuccessStatusCode();
        var finalResult = await finalResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<OpenMeteoResponse>>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return finalResult ?? [];
    }
}
