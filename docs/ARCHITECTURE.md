# Архитектура

## Назначение

Боковая панель на рабочем столе Windows (по умолчанию справа, в V6 можно прилепить к левому или правому краю): кольцевые индикаторы CPU, RAM, GPU, сети; в V5+ — охранное кольцо по удалённому доступу; в V6 — отдельное окно сетевых подключений и захват пакетов (с Npcap).

Стек: **.NET Framework 4.8.1**, **WinForms**, сбор метрик через `PerformanceCounter`, WMI, WTS, TCP tables, журнал Security (где доступен).

## Проекты решения

| Проект | Назначение |
|--------|------------|
| **SidePanelApp** (V1) | Общий код: `SystemMetricsProvider`, `AppIconFactory` |
| **SidePanelApp.V2** | UI из контролов, `MainForm` с настройками |
| **SidePanelApp.V3** | Как V2, запуск из трея |
| **SidePanelApp.V4** | V3 + GPU, NIC, детальные попапы по устройствам |
| **SidePanelApp.V5** | V4 + кольцо мониторинга АРМ (см. [THREAT_MONITOR.md](./THREAT_MONITOR.md)) |
| **SidePanelApp.V6** | **Текущая линия:** V5 + фоновый `ThreatBackgroundService`, `EventLogWatcher`, `SeriesInstanceGuard`, окно **Connections** |
| **GenerateAppIcon** | Утилита: записать `Assets/app.ico` из `AppIconFactory` |

Общий код из V1 подключается в V2–V6 через `<Compile Include=".." Link="...">` в `.csproj`.

## SidePanelApp.V6 (основной)

### Точка входа и трей

- `Program.cs` → `Application.Run(new TrayAppContext())`.
- `TrayAppContext` — иконка в трее, меню (показать панель, автозапуск Windows, показ при старте, перемещение панели, соединения), создаёт `OverlayPanelV6` и `ThreatBackgroundService`. ЛКМ по иконке показывает или скрывает панель.

### Панель

- `OverlayPanelV6` — узкая форма; кольца метрик и `ThreatRingIndicator`. Таймер UI (~1 с) ставит сбор в фон (`Hardware/*MetricsCollector` + `GetLatestSnapshot()` угроз) и применяет снимок на UI-потоке.
- Старт и режим без перемещения: панель у **правого** края, скругление слева (`CreateLeftRoundedRect`).
- С включённым «Разрешить перемещение панели» во время drag форма магнитится к краю, если до него ≤ 32 px (`EdgeSnapDistance`); после отпускания — к ближайшему вертикальному краю рабочей области (`SnapPanelToEdge`). У правого края скругление слева, у левого — справа (`CreateRightRoundedRect`). По вертикали позиция сохраняется, с ограничением экраном.

### Выноски колец

Hover/клик открывает попап рядом с кольцом. Сторона выбирается по тому, к какому краю ближе панель (`IndicatorPopupAlignment.ResolveTailSide`):

| Панель | Попап | Клюв (`CalloutTailSide`) |
|--------|-------|--------------------------|
| Справа | Слева от панели | Справа (`Right`), контур `CreateCalloutPathTailRight` |
| Слева | Справа от панели | Слева (`Left`): тот же контур, отражённый по X |

После прилипания видимые попапы переставляются (`RepositionVisibleDetailPopups`). Контент карточек (CPU, RAM, устройства, угрозы) сдвигается на ширину клюва через `CalloutContentBounds`.

### Мониторинг АРМ

- `ThreatBackgroundService` — цикл в `Task.Run`, параллельные пробы каналов, интервал из настроек (`ThreatPollIntervalMs`).
- Сборщики и модели в `Security/` (наследие V5, доработано для фона и watcher).

Подробности каналов и настроек: [THREAT_MONITOR.md](./THREAT_MONITOR.md).

### Одна копия серии

`SeriesInstanceGuard` при старте ищет процессы `SidePanelApp`, `SidePanelApp.V2` … `SidePanelApp.V6` и предлагает закрыть чужие экземпляры.

### Автозапуск

- Пункт трея «Запускать при входе в Windows» — ярлык в `Shell:Startup` пользователя (не HKLM).
- При первом запуске V6 может включить автозагрузку и показ панели (`V6StartupConfigured`); старые ярлыки V1–V5 удаляются.
- Манифест `asInvoker` — без обязательного UAC при старте.

### Connections

Отдельная форма `ConnectionsForm` — таблица соединений, фильтры, детали, HEX/ASCII по пакетам при установленном Npcap. См. [CONNECTIONS.md](./CONNECTIONS.md).

## UI-паттерн (V2+, для V6 — схема ниже)

```
TrayAppContext
        │
        ▼
OverlayPanelV6  ──► RingIndicatorControl × N, ThreatRingIndicator
        │              hover/click → *DetailPopup
        │                              └── IndicatorPopupAlignment (сторона и клюв)
        ├── PanelColors (палитра, скругление панели, контур клюва)
        ├── Hardware/*MetricsCollector (фон, ~1 с)
        └── ThreatBackgroundService.GetLatestSnapshot()
```

В V1–V4 метрики идут через общий `SystemMetricsProvider`. В V6 сбор свой: `Hardware/`.

Расширение новой метрики: `RingIndicatorControl` в designer, коллектор в `CollectMetricsBundle`, попап с `TailSide`.

## Иконка приложения

| Что | Где |
|-----|-----|
| Файл для exe и встраивания | `SidePanelApp/Assets/app.ico` (16 / 32 / 48) |
| Рисование | `SidePanelApp/AppIconFactory.cs` |
| Генератор | `tools/GenerateAppIcon.csproj` → `tools/bin/<Config>/GenerateAppIcon.exe` |

`Directory.Build.targets` перед сборкой приложений: если нет exe генератора или нет `app.ico` — собирает утилиту и при необходимости создаёт иконку.

Ручная пересборка иконки после правок `AppIconFactory`:

```text
msbuild tools\GenerateAppIcon.csproj
tools\bin\Debug\GenerateAppIcon.exe Assets\app.ico
```

## Сборка

Solution: `SidePanelApp/SidePanelApp.sln`. Стартовый проект для работы: **SidePanelApp.V6**.

## Исходные требования (статус)

| Требование | Статус |
|------------|--------|
| Каждый «кругляш» — отдельный контрол | ✅ |
| Закрепление / открепление панели | ✅ `AllowPanelMove` |
| Hover → выноска с баром ресурса | ✅ |
| Закруглённые края панели | ✅ |
| Прилипание к левому или правому краю, клюв попапа в сторону панели | ✅ V6 |
| Метрика сети | ✅ V4+ |
| CPU по ядрам в выноске | ✅ V4+ |
| Кто подключился к АРМ | ✅ V5/V6 — [THREAT_MONITOR.md](./THREAT_MONITOR.md) |
| Таблица подключений / пакеты | ✅ V6 — [CONNECTIONS.md](./CONNECTIONS.md) |
