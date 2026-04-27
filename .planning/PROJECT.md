# Bulgaria Weather Map

## What This Is

Уеб приложение, показващо текущите температури в 28-те областни града на България върху интерактивна карта. Всеки град е маркиран с икона, показваща реалната текуща температура. Данните идват от реален weather API (Open-Meteo), проксиран от ASP.NET Core бекенда.

_A web application showing current temperatures in all 28 Bulgarian district cities on an interactive map. Each city has a marker displaying the live temperature fetched via the ASP.NET Core proxy backend from a real weather API (Open-Meteo)._

## Core Value

Потребителят вижда наведнъж реалните температури в 28-те областни града на България на карта — без излишно.

_At-a-glance view of current temperatures across all Bulgarian district cities on a single map._

## Requirements

### Validated

- ✓ ASP.NET Core 8 Web API project exists with `/weatherforecast` endpoint — existing (will be extended)

### Active

- [ ] **MAP-01**: Показва интерактивна карта на България (Leaflet, OpenStreetMap tiles), центрирана върху страната
- [ ] **MAP-02**: 28 маркера — по един на всеки областен град с точни координати
- [ ] **MAP-03**: Всеки маркер показва текущата температура (°C) директно върху картата
- [ ] **API-01**: ASP.NET Core бекенд добавя ендпоинт `/cities/temperatures` — връща текущата температура за всички 28 града
- [ ] **API-02**: Бекендът проксира Open-Meteo (или OpenWeatherMap) — ключовете/конфигурацията са сървърна страна
- [ ] **UI-01**: React SPA фронтенд (Vite + React 19 + TypeScript + Tailwind CSS + react-leaflet)
- [ ] **UI-02**: Минимален, чист дизайн — картата е главният елемент, без странични панели или разсейващи елементи
- [ ] **UI-03**: Loading state докато данните се зареждат; error state при грешка
- [ ] **CORS-01**: CORS политика в бекенда позволява заявки от Vite dev сървъра (localhost:5173)

### Out of Scope

- 5-дневна прогноза — потребителят иска само текуща температура
- Popup с допълнителни детайли (влажност, вятър и т.н.) — температурата е достатъчна
- Тъмен/атмосферен дизайн — предпочетен минимален/чист стил
- Автентикация — публично приложение без login

## Context

- **Съществуващ бекенд**: ASP.NET Core 8 в `src/WeatherForecast/` — работи на `http://localhost:5055`. Сега връща случайни данни; ще се разшири с реален weather API прокси.
- **28 областни града**: Благоевград, Бургас, Варна, Велико Търново, Видин, Враца, Габрово, Добрич, Кърджали, Кюстендил, Ловеч, Монтана, Пазарджик, Перник, Плевен, Пловдив, Разград, Русе, Силистра, Сливен, Смолян, София, Стара Загора, Търговище, Хасково, Шумен, Ямбол (27 + Sofia-grad = 28)
- **Weather API**: Open-Meteo е безплатен без API ключ — идеален избор. Поддържа bulk заявки за множество координати.
- **Фронтенд**: Нов React проект в `src/weather-ui/` — все още не е създаден.

## Constraints

- **Tech Stack**: ASP.NET Core 8 + .NET 8 за бекенд — не сменяме платформата
- **Tech Stack**: React 19 + Vite + TypeScript за фронтенд — от backlog спецификацията
- **Map Library**: Leaflet чрез react-leaflet — вече е избрано
- **No API Key**: Open-Meteo не изисква ключ — предпочетено пред OpenWeatherMap
- **Nullable refs**: Nullable reference types са включени в .NET проекта

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Open-Meteo за weather данни | Безплатен, без API ключ, поддържа bulk координати | — Pending |
| ASP.NET Core като прокси | Централизирано управление на external calls, CORS избегнат | — Pending |
| Само текуща температура | Потребителят е избрал минимален подход | — Pending |
| Leaflet + react-leaflet за картата | Лек, добре интегриран с React | — Pending |
| Минимален/чист дизайн | Картата трябва да е главният елемент | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-27 after initialization*
