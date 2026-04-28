namespace WeatherForecast;

/// <summary>Static list of all 28 Bulgarian district (oblast) administrative centers.</summary>
public static class BulgarianCities
{
    /// <summary>
    /// All 28 Bulgarian district city entries in a stable, deterministic order.
    /// This order MUST remain consistent — Open-Meteo bulk responses are joined by positional index.
    /// </summary>
    public static readonly IReadOnlyList<CityInfo> All = new[]
    {
        new CityInfo("Благоевград",    "Blagoevgrad",    42.0147, 23.0958),
        new CityInfo("Бургас",         "Burgas",         42.5048, 27.4626),
        new CityInfo("Варна",          "Varna",          43.2048, 27.9100),
        new CityInfo("Велико Търново", "Veliko Tarnovo", 43.0757, 25.6172),
        new CityInfo("Видин",          "Vidin",          43.9956, 22.8742),
        new CityInfo("Враца",          "Vratsa",         43.2003, 23.5561),
        new CityInfo("Габрово",        "Gabrovo",        42.8742, 25.3175),
        new CityInfo("Добрич",         "Dobrich",        43.5667, 27.8278),
        new CityInfo("Кърджали",       "Kardzhali",      41.6417, 25.3678),
        new CityInfo("Кюстендил",      "Kyustendil",     42.2833, 22.6972),
        new CityInfo("Ловеч",          "Lovech",         43.1364, 24.7175),
        new CityInfo("Монтана",        "Montana",        43.4083, 23.2256),
        new CityInfo("Пазарджик",      "Pazardzhik",     42.1928, 24.3275),
        new CityInfo("Перник",         "Pernik",         42.6053, 23.0375),
        new CityInfo("Плевен",         "Pleven",         43.4175, 24.6167),
        new CityInfo("Пловдив",        "Plovdiv",        42.1500, 24.7500),
        new CityInfo("Разград",        "Razgrad",        43.5256, 26.5228),
        new CityInfo("Русе",           "Ruse",           43.8419, 25.9558),
        new CityInfo("Силистра",       "Silistra",       44.1167, 27.2667),
        new CityInfo("Сливен",         "Sliven",         42.6836, 26.3228),
        new CityInfo("Смолян",         "Smolyan",        41.5769, 24.7114),
        new CityInfo("София",          "Sofia",          42.6977, 23.3219),
        new CityInfo("Софийска",       "Sofia Province", 42.7333, 23.6167),
        new CityInfo("Стара Загора",   "Stara Zagora",   42.4257, 25.6344),
        new CityInfo("Търговище",      "Targovishte",    43.2539, 26.5681),
        new CityInfo("Хасково",        "Haskovo",        41.9344, 25.5553),
        new CityInfo("Шумен",          "Shumen",         43.2706, 26.9208),
        new CityInfo("Ямбол",          "Yambol",         42.4836, 26.5103),
    }.AsReadOnly();
}

/// <summary>A Bulgarian district city with WGS84 coordinates and both name variants.</summary>
/// <param name="NameNative">City name in Bulgarian Cyrillic script (e.g. "София").</param>
/// <param name="NameEn">City name in Latin transliteration (e.g. "Sofia").</param>
/// <param name="Latitude">WGS84 latitude in decimal degrees.</param>
/// <param name="Longitude">WGS84 longitude in decimal degrees.</param>
public record CityInfo(string NameNative, string NameEn, double Latitude, double Longitude);
