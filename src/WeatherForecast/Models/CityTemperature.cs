namespace WeatherForecast;

/// <summary>
/// API response DTO for a single Bulgarian district city with its current temperature.
/// Returned as an array by GET /api/cities/temperatures.
/// </summary>
public class CityTemperature
{
    /// <summary>City name in Bulgarian Cyrillic script (e.g. "София").</summary>
    public string NameNative { get; set; } = string.Empty;

    /// <summary>City name in Latin transliteration (e.g. "Sofia").</summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>WGS84 latitude in decimal degrees.</summary>
    public double Latitude { get; set; }

    /// <summary>WGS84 longitude in decimal degrees.</summary>
    public double Longitude { get; set; }

    /// <summary>Current temperature in degrees Celsius from Open-Meteo. Null if Open-Meteo omitted this city.</summary>
    public double? TemperatureC { get; set; }
}
