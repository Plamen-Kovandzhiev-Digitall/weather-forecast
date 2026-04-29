import { Component, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
}

export class MapErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="absolute inset-0 z-[1000] bg-black/45 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-2xl p-6 max-w-sm w-full flex flex-col gap-4 border-l-4 border-red-500">
            <span className="text-3xl">⚠️</span>
            <h2 className="text-lg font-bold text-gray-900">Unable to load weather data</h2>
            <p className="text-sm text-gray-600 leading-relaxed">
              The map failed to load. Please refresh the page.
            </p>
            <button
              onClick={() => window.location.reload()}
              className="bg-blue-600 hover:bg-blue-700 text-white text-sm font-bold px-4 py-2 rounded"
            >
              Refresh
            </button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
