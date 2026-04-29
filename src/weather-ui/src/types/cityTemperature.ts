export interface CityTemperature {
  nameNative:   string;   // Bulgarian city name e.g. "София"
  nameEn:       string;   // English city name e.g. "Sofia"
  latitude:     number;
  longitude:    number;
  temperatureC: number;   // backend returns double? — handle null/NaN at render time, not here
}
