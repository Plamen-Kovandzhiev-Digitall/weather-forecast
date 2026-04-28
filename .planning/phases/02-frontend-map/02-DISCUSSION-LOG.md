# Phase 2: Frontend Map - Discussion Log

**Date:** 2026-04-28
**Phase:** 02-frontend-map
**Areas discussed:** 4

---

## Area 1: Tailwind version

**Question:** Upgrade Tailwind v3 → v4 during the rewrite, or stay on v3?

**Options presented:**
- Upgrade to v4 — clean slate since we're rewriting anyway
- Stay on v3 — no migration risk, fully functional

**User selected:** Upgrade to v4

**Notes:** Project currently on v3.4.17 with postcss.config.js. Since all CSS is being rewritten anyway, migration cost is minimal. v4 uses @tailwindcss/vite plugin, drops postcss and tailwind.config.js.

---

## Area 2: App chrome

**Question:** How much chrome should wrap the map?

**Options presented:**
- Pure full-viewport — map fills 100% of the screen, Leaflet attribution only
- Minimal floating header — slim translucent title bar overlaid on the map
- Keep header + footer — dedicated bands above and below the map

**User selected:** Pure full-viewport

**Notes:** Current app has "LIVE WEATHER / VELIKO TARNOVO" header and footer. Phase 2 requirements (UI-04) say map is the hero element. Decision aligns with minimal/clean design preference from PROJECT.md.

---

## Area 3: Loading & error states

**Question:** What should the user see while loading and when an error occurs?

**Options presented:**
- Spinner overlay on map + centered error card
- Empty map (no loading UI) + error banner at top
- Full-screen spinner + centered error card
- Spinner overlay on map + corner toast for errors

**User selected:** Spinner overlay on map + centered error card

**Notes:** Map tiles render immediately (good perceived performance). Spinner overlay keeps context. Error card is prominent enough to clearly communicate failure without being dismissive.

---

## Area 4: Temperature precision

**Question:** How should temperatures be displayed on the marker badges?

**Options presented:**
- Integer — round to nearest whole number (19°C)
- One decimal — show as-is from API (18.7°C)

**User selected:** Integer (Math.round)

**Notes:** Marker badges have limited space at zoom level 7. Integer display is cleaner and more readable. The extra decimal precision is not meaningful for at-a-glance weather viewing.

---

## Deferred Ideas

- "Last updated" timestamp — v2 requirement
- Temperature unit toggle — v2 requirement

---

*Discussion completed: 2026-04-28*
