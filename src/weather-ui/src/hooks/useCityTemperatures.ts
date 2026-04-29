import { useState, useEffect } from 'react';
import type { CityTemperature } from '../types/cityTemperature';

interface UseCityTemperaturesResult {
  data: CityTemperature[] | null;
  loading: boolean;
  error: string | null;
  retry: () => void;
}

export function useCityTemperatures(): UseCityTemperaturesResult {
  const [data, setData] = useState<CityTemperature[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    let cancelled = false;

    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        const response = await fetch('/api/cities/temperatures');
        if (!response.ok) {
          throw new Error(`API error: ${response.status} ${response.statusText}`);
        }
        const json: unknown = await response.json();
        if (!Array.isArray(json)) {
          throw new Error('Unexpected response format from weather API');
        }
        if (!cancelled) setData(json as CityTemperature[]);
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to fetch temperature data');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    fetchData();
    return () => { cancelled = true; };
  }, [retryCount]);

  const retry = () => setRetryCount(c => c + 1);

  return { data, loading, error, retry };
}
