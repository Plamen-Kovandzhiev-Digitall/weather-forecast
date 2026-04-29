import { useState, useEffect } from 'react';
import type { CityTemperature } from '../types/cityTemperature';

interface UseCityTemperaturesResult {
  data: CityTemperature[] | null;
  loading: boolean;
  error: string | null;
  retry: () => void;
}

function isCityTemperature(item: unknown): item is CityTemperature {
  if (typeof item !== 'object' || item === null) return false;
  const o = item as Record<string, unknown>;
  return (
    typeof o.nameNative === 'string' &&
    typeof o.nameEn     === 'string' &&
    typeof o.latitude   === 'number' &&
    typeof o.longitude  === 'number' &&
    (o.temperatureC === null || typeof o.temperatureC === 'number')
  );
}

export function useCityTemperatures(): UseCityTemperaturesResult {
  const [data, setData] = useState<CityTemperature[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        const response = await fetch('/api/cities/temperatures', {
          signal: controller.signal,
        });
        if (!response.ok) {
          throw new Error(`API error: ${response.status} ${response.statusText}`);
        }
        const json: unknown = await response.json();
        if (!Array.isArray(json)) {
          throw new Error('Unexpected response format from weather API');
        }
        const validated = json.filter(isCityTemperature);
        setData(validated);
      } catch (err) {
        if (err instanceof DOMException && err.name === 'AbortError') return;
        setError(err instanceof Error ? err.message : 'Failed to fetch temperature data');
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    };

    fetchData();
    return () => controller.abort();
  }, [retryCount]);

  const retry = () => setRetryCount(c => c + 1);

  return { data, loading, error, retry };
}
