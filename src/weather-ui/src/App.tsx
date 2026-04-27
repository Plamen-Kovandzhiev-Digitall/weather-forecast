import { useWeatherData } from './hooks/useWeatherData';
import { WeatherMap } from './components/WeatherMap';
import './App.css';

export default function App() {
  const { data, loading, error } = useWeatherData();

  return (
    <div className="app-shell">
      <header className="app-header" role="banner">
        <div className="header-inner">
          <div className="header-eyebrow">LIVE WEATHER</div>
          <h1 className="header-title">VELIKO TARNOVO</h1>
          <div className="header-sub">Bulgaria · 43°N 25°E</div>
        </div>
        <div className="header-status" aria-live="polite">
          {loading && <span className="status-badge loading">● FETCHING DATA</span>}
          {error && <span className="status-badge error">● API OFFLINE</span>}
          {data && !loading && <span className="status-badge ok">● LIVE</span>}
        </div>
      </header>
      <main className="app-main">
        <WeatherMap forecasts={data} loading={loading} error={error} />
      </main>
      <footer className="app-footer">
        <span>Map data © OpenStreetMap contributors</span>
        <span>Forecast via local ASP.NET API</span>
      </footer>
    </div>
  );
}
