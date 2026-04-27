import type { WeatherForecast } from '../types/weather';

interface WeatherPopupProps {
  forecasts: WeatherForecast[];
}

const SUMMARY_ICONS: Record<string, string> = {
  Freezing: '❄️',
  Bracing: '🌬️',
  Chilly: '🌥️',
  Cool: '🌤️',
  Mild: '🌦️',
  Warm: '☀️',
  Balmy: '🌅',
  Hot: '🔥',
  Sweltering: '🌡️',
  Scorching: '☄️',
};

function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'short' });
}

function tempBar(tempC: number): number {
  // Normalize -20..45 to 0..100
  return Math.max(0, Math.min(100, ((tempC + 20) / 65) * 100));
}

function tempColor(tempC: number): string {
  if (tempC <= 0) return '#60a5fa';
  if (tempC <= 10) return '#93c5fd';
  if (tempC <= 20) return '#6ee7b7';
  if (tempC <= 30) return '#fbbf24';
  return '#f87171';
}

export function WeatherPopup({ forecasts }: WeatherPopupProps) {
  return (
    <div className="weather-popup">
      <div className="popup-header">
        <span className="popup-city">VELIKO TARNOVO</span>
        <span className="popup-label">5-DAY FORECAST</span>
      </div>
      <div className="popup-rows">
        {forecasts.map((f) => {
          const icon = SUMMARY_ICONS[f.summary] ?? '🌈';
          const bar = tempBar(f.temperatureC);
          const color = tempColor(f.temperatureC);
          return (
            <div key={f.date} className="popup-row">
              <span className="popup-icon">{icon}</span>
              <span className="popup-date">{formatDate(f.date)}</span>
              <div className="popup-bar-wrap">
                <div
                  className="popup-bar"
                  style={{ width: `${bar}%`, backgroundColor: color }}
                />
              </div>
              <span className="popup-temps">
                <span className="temp-c">{f.temperatureC}°C</span>
                <span className="temp-sep">/</span>
                <span className="temp-f">{f.temperatureF}°F</span>
              </span>
              <span className="popup-summary">{f.summary}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
