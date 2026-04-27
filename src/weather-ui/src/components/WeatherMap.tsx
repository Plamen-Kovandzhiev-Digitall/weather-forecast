import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { WeatherForecast } from '../types/weather';
import { WeatherPopup } from './WeatherPopup';

// Fix Leaflet default marker icons for bundlers
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
});

const VT_COORDS: [number, number] = [43.0757, 25.6172];

interface WeatherMapProps {
  forecasts: WeatherForecast[] | null;
  loading: boolean;
  error: string | null;
}

export function WeatherMap({ forecasts, loading, error }: WeatherMapProps) {
  return (
    <div className="map-wrapper">
      <MapContainer
        center={VT_COORDS}
        zoom={12}
        scrollWheelZoom
        className="leaflet-map"
        aria-label="Map centered on Veliko Tarnovo, Bulgaria"
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <Marker position={VT_COORDS}>
          <Popup minWidth={340} maxWidth={400} className="vt-popup">
            {loading && (
              <div className="popup-state">
                <div className="spinner" aria-label="Loading forecast..." />
                <span>Loading forecast…</span>
              </div>
            )}
            {error && (
              <div className="popup-state popup-error">
                <span>⚠️ {error}</span>
              </div>
            )}
            {forecasts && !loading && !error && (
              <WeatherPopup forecasts={forecasts} />
            )}
          </Popup>
        </Marker>
      </MapContainer>
    </div>
  );
}
