import { useMemo } from 'react';
import { Marker, Tooltip } from 'react-leaflet';
import L from 'leaflet';
import type { CityTemperature } from '../types/cityTemperature';

// NOTE: 'leaflet/dist/leaflet.css' is imported in BulgariaMap.tsx — do NOT import it here

function getTempColorClass(temp: number): string {
  if (isNaN(temp)) return 'bg-gray-400';   // no-data colour — explicit gray instead of falling through to red
  if (temp <= 0)  return 'bg-blue-500';   // ≤0°C  (D-09)
  if (temp <= 15) return 'bg-teal-500';   // 1–15°C (D-09)
  if (temp <= 25) return 'bg-green-500';  // 16–25°C (D-09)
  if (temp <= 35) return 'bg-orange-500'; // 26–35°C (D-09)
  return 'bg-red-500';                    // >35°C  (D-09)
}

function createTempIcon(temp: number): L.DivIcon {
  const colorClass = getTempColorClass(temp);
  const label = isNaN(temp) ? '—' : `${temp}°C`;  // null/NaN guard (UI-SPEC data contract)
  return L.divIcon({
    className: '',  // D-07: CRITICAL — clears leaflet-div-icon default white box so Tailwind controls all styling
    html: `<div class="${colorClass} text-white text-[13px] font-bold px-2 py-1 rounded shadow-md whitespace-nowrap leading-none select-none">${label}</div>`,
    iconSize:   [52, 28],  // UI-SPEC §2: bounding box for hit-test and centering
    iconAnchor: [26, 14], // UI-SPEC §2: center of badge = exact city coordinate (half of iconSize)
  });
}

export function CityMarker({ city }: { city: CityTemperature }) {
  const roundedTemp = Math.round(city.temperatureC ?? NaN);  // D-08: integer display; ?? NaN handles null/undefined

  // PITFALL §6: icon MUST be memoized — never create L.divIcon inline in JSX
  // Recreated only when temperature changes, not on every parent re-render
  const icon = useMemo(
    () => createTempIcon(roundedTemp),
    [roundedTemp]
  );

  return (
    // PITFALL §13: use city.latitude/city.longitude (canonical backend coords), not Open-Meteo snapped coords
    <Marker position={[city.latitude, city.longitude]} icon={icon}>
      <Tooltip direction="top" permanent={false} opacity={0.9}>
        {city.nameEn}
      </Tooltip>
    </Marker>
  );
}
