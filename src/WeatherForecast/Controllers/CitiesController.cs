namespace WeatherForecast;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides current temperature readings for all 28 Bulgarian district cities.
/// Data sourced from Open-Meteo, cached for 10 minutes.
/// </summary>
[ApiController]
[Route("api/cities")]
public class CitiesController(
    ICityTemperatureService cityTemperatureService,
    ILogger<CitiesController> logger) : ControllerBase
{
    /// <summary>
    /// Returns current temperatures for all 28 Bulgarian district administrative centers.
    /// Results are cached for up to 10 minutes.
    /// </summary>
    /// <response code="200">Array of city temperature readings ordered by administrative center name.</response>
    /// <response code="503">Upstream Open-Meteo service unavailable after 3 retry attempts.</response>
    [HttpGet("temperatures")]
    [ProducesResponseType(typeof(IReadOnlyList<CityTemperature>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetTemperatures(CancellationToken cancellationToken)
    {
        try
        {
            var temperatures = await cityTemperatureService
                .GetAllAsync(cancellationToken)
                .ConfigureAwait(false);
            return Ok(temperatures);
        }
        catch (Exception ex)
        {
            // D-02: structured 503 on upstream failure after all retries exhausted
            logger.LogError(ex, "Failed to retrieve city temperatures from Open-Meteo.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error   = "upstream_failure",
                message = "Could not retrieve temperature data from Open-Meteo. Please try again later.",
            });
        }
    }
}
