import { useCityTemperatures } from './hooks/useCityTemperatures';
import { BulgariaMap } from './components/BulgariaMap';
import { MapErrorBoundary } from './components/MapErrorBoundary';
// No App.css import — D-04 deleted App.css; all styles via Tailwind v4 utilities in index.css

export default function App() {
  const { data, loading, error, retry } = useCityTemperatures();

  return (
    // D-03: full-viewport shell, no header/footer chrome; map is the only persistent UI element
    // UI-SPEC Layout Contract: relative positioning context for absolute overlays
    <div className="relative w-screen h-screen overflow-hidden">

      {/* MAP: always rendered so tiles start loading immediately — visible beneath any overlay (D-11) */}
      <MapErrorBoundary>
        <BulgariaMap cities={data} />
      </MapErrorBoundary>

      {/* LOADING OVERLAY: absolute inset-0, z-[1000] — above all Leaflet layers (D-11, D-13) */}
      {loading && (
        <div className="absolute inset-0 z-[1000] bg-black/45 flex items-center justify-center">
          <div
            className="w-10 h-10 rounded-full border-4 border-white/20 border-t-white animate-spin"
            role="status"
            aria-label="Loading temperature data…"
          />
        </div>
      )}

      {/* ERROR OVERLAY: absolute inset-0, z-[1000] — above all Leaflet layers (D-12, D-13) */}
      {error && (
        <div className="absolute inset-0 z-[1000] bg-black/45 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-2xl p-6 max-w-sm w-full flex flex-col gap-4 border-l-4 border-red-500">
            <span className="text-3xl">⚠️</span>
            <h2 className="text-lg font-bold text-gray-900">Unable to load weather data</h2>
            <p className="text-sm text-gray-600 leading-relaxed">
              The weather service is unavailable. Make sure the backend is running at localhost:5055, then try again.
            </p>
            <button
              onClick={retry}
              className="bg-blue-600 hover:bg-blue-700 text-white text-sm font-bold px-4 py-2 rounded"
            >
              Try again
            </button>
          </div>
        </div>
      )}

    </div>
  );
}
