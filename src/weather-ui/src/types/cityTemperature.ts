export interface CityTemperature {
  nameNative:   string;   // Bulgarian city name e.g. "София"
  nameEn:       string;   // English city name e.g. "Sofia"
  latitude:     number;
  longitude:    number;
  temperatureC: number | null;   // backend double? can be null when no reading available
}
