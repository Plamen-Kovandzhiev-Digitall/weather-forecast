---
phase: 02-frontend-map
plan: "01"
subsystem: frontend-toolchain
tags: [tailwind-v4, vite, css, toolchain-migration]
dependency_graph:
  requires: []
  provides: [tailwind-v4-setup, vite-api-proxy]
  affects: [src/weather-ui/package.json, src/weather-ui/vite.config.ts, src/weather-ui/src/index.css]
tech_stack:
  added: [tailwindcss@4.2.4, "@tailwindcss/vite@4.2.4"]
  removed: [autoprefixer, postcss, tailwindcss@3]
  patterns: [tailwind-v4-css-first-import, vite-proxy]
key_files:
  created: []
  modified:
    - src/weather-ui/package.json
    - src/weather-ui/vite.config.ts
    - src/weather-ui/src/index.css
  deleted:
    - src/weather-ui/tailwind.config.js
    - src/weather-ui/postcss.config.js
    - src/weather-ui/src/App.css
    - src/weather-ui/src/types/weather.ts
    - src/weather-ui/src/hooks/useWeatherData.ts
    - src/weather-ui/src/components/WeatherMap.tsx
    - src/weather-ui/src/components/WeatherPopup.tsx
decisions:
  - "Moved @tailwindcss/vite to devDependencies (npm defaulted to dependencies for new package)"
metrics:
  duration: "~10 minutes"
  completed: "2026-04-28"
  tasks_completed: 2
  tasks_total: 2
---

# Phase 2 Plan 01: Tailwind v4 Toolchain Migration Summary

**One-liner:** Migrated frontend from Tailwind v3 PostCSS pipeline to Tailwind v4 Vite-native plugin, added `/api` proxy, and deleted 7 obsolete source files incompatible with new API shape.

## What Was Built

Established the correct toolchain foundation for all subsequent Phase 2 plans:

1. **Tailwind v4 upgrade** — replaced `tailwindcss@3` + `autoprefixer` + `postcss` with `tailwindcss@4` + `@tailwindcss/vite` (both in `devDependencies`)
2. **vite.config.ts rewrite** — wired `@tailwindcss/vite` plugin alongside `@vitejs/plugin-react`; added `server.proxy` routing `/api` → `http://localhost:5055` (eliminates HTTPS redirect and CORS dependency per PITFALLS §4/§8)
3. **index.css rewrite** — replaced `@tailwind base/components/utilities` directives with `@import "tailwindcss"` (v4 CSS-first syntax); removed `:root` custom properties (old dark theme vars, D-04); retained box-sizing and `html/body/#root` full-viewport reset
4. **Obsolete files deleted** — 7 files removed: `tailwind.config.js`, `postcss.config.js`, `App.css`, `types/weather.ts`, `hooks/useWeatherData.ts`, `components/WeatherMap.tsx`, `components/WeatherPopup.tsx`

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 | `b905c25` | chore(02-01): upgrade Tailwind v4, remove v3 toolchain and obsolete source files |
| Task 2 | `2b198d7` | feat(02-01): wire Tailwind v4 plugin and /api proxy in vite.config; v4 CSS import in index.css |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing] @tailwindcss/vite placed in devDependencies**
- **Found during:** Task 1 verification
- **Issue:** `npm install tailwindcss@latest @tailwindcss/vite@latest` (without `--save-dev`) caused `@tailwindcss/vite` to land in `dependencies` instead of `devDependencies`. `tailwindcss` itself updated in-place in devDependencies (it already existed there), but `@tailwindcss/vite` was new and npm defaulted it to runtime.
- **Fix:** Ran `npm uninstall @tailwindcss/vite && npm install --save-dev @tailwindcss/vite@latest` to move it to devDependencies.
- **Files modified:** `src/weather-ui/package.json`, `src/weather-ui/package-lock.json`

## Known Stubs

None — this plan is toolchain-only with no UI rendering. No stubs introduced.

## Threat Flags

None — no new network endpoints or auth paths introduced beyond the Vite dev proxy already documented in the plan's threat model (T-01-01: accepted risk, development-only).

## Self-Check: PASSED

- ✅ `src/weather-ui/vite.config.ts` — exists, contains `@tailwindcss/vite` and `localhost:5055`
- ✅ `src/weather-ui/src/index.css` — exists, contains `@import "tailwindcss"` and `html, body, #root`
- ✅ `src/weather-ui/package.json` — `tailwindcss@^4.2.4` and `@tailwindcss/vite@^4.2.4` in devDependencies; no autoprefixer/postcss
- ✅ All 7 obsolete files deleted
- ✅ Commit `b905c25` — Task 1
- ✅ Commit `2b198d7` — Task 2
