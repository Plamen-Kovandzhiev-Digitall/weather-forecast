import { MapContainer, TileLayer } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { CityTemperature } from '../types/cityTemperature';
import { CityMarker } from './CityMarker';

// Leaflet default marker icon fix for bundlers (Vite/webpack asset hashing breaks default icon URLs)
// Harmless when using DivIcon-only markers, but prevents broken image errors if a fallback Marker is ever introduced
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
});

// D-05: immutable after mount — set correctly from the start (PITFALL §7)
const BULGARIA_CENTER: [number, number] = [42.7, 25.5];

interface BulgariaMapProps {
  cities: CityTemperature[] | null;
}

export function BulgariaMap({ cities }: BulgariaMapProps) {
  return (
    // D-03: full-viewport wrapper; aria-label identifies the map for screen readers (UI-SPEC accessibility)
    <div
      className="h-screen w-screen"
      aria-label="Bulgaria weather map showing current temperatures for 28 district cities"
    >
      {/* D-05 / PITFALL §7: center and zoom are immutable — never pass them as dynamic state */}
      <MapContainer
        center={BULGARIA_CENTER}
        zoom={7}
        scrollWheelZoom
        className="h-full w-full"
      >
        {/* D-06: CartoDB Positron tiles — clean minimal basemap */}
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/">CARTO</a>'
          url="https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png"
        />
        {/* MAP-02: render one CityMarker per city; key on nameEn (unique English city names) */}
        {cities?.map((city) => (
          <CityMarker key={city.nameEn} city={city} />
        ))}
      </MapContainer>
    </div>
  );
}
