namespace WeatherForecast;

using System.Text.Json.Serialization;

/// <summary>
/// Deserialization model for one element of the Open-Meteo bulk forecast response array.
/// Properties use JsonPropertyName to match Open-Meteo's snake_case JSON keys.
/// </summary>
public class OpenMeteoResponse
{
    /// <summary>Latitude as snapped to Open-Meteo's internal grid (~7 km resolution). Do NOT use for marker positioning — use BulgarianCities coordinates instead (PITFALLS #13).</summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>Longitude as snapped to Open-Meteo's internal grid. Do NOT use for marker positioning.</summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    /// <summary>Current weather block. Nullable because Open-Meteo may omit this on error responses (PITFALLS #10).</summary>
    [JsonPropertyName("current")]
    public OpenMeteoCurrent? Current { get; set; }
}

/// <summary>The "current" block of an Open-Meteo response element.</summary>
public class OpenMeteoCurrent
{
    /// <summary>Current temperature at 2 metres above ground in degrees Celsius. Use &amp;current=temperature_2m in the request (PITFALLS #2).</summary>
    [JsonPropertyName("temperature_2m")]
    public double Temperature2M { get; set; }

    /// <summary>Observation time in ISO 8601 format.</summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>Update interval in seconds (typically 900 = 15 minutes).</summary>
    [JsonPropertyName("interval")]
    public int Interval { get; set; }
}
