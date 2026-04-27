import { useState, useEffect } from 'react';
import type { WeatherForecast } from '../types/weather';

interface UseWeatherDataResult {
  data: WeatherForecast[] | null;
  loading: boolean;
  error: string | null;
}

export function useWeatherData(): UseWeatherDataResult {
  const [data, setData] = useState<WeatherForecast[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        const response = await fetch('http://localhost:5055/weatherforecast');
        if (!response.ok) {
          throw new Error(`API error: ${response.status} ${response.statusText}`);
        }
        const json: WeatherForecast[] = await response.json();
        if (!cancelled) setData(json);
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to fetch forecast data');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    fetchData();
    return () => { cancelled = true; };
  }, []);

  return { data, loading, error };
}
