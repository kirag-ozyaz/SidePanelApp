# Архитектура

## Назначение

Боковая панель справа на рабочем столе Windows: кольцевые индикаторы CPU, RAM, GPU, сети; в V5+ — охранное кольцо по удалённому доступу; в V6 — отдельное окно сетевых подключений и захват пакетов (с Npcap).

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
- `TrayAppContext` — иконка в трее, меню (показать панель, автозапуск Windows, показ при старте, перемещение панели), создаёт `OverlayPanelV6` и `ThreatBackgroundService`.

### Панель

- `OverlayPanelV6` — узкая форма у правого края; кольца метрик и `ThreatRingIndicator`; таймер UI (~1 с) читает снимок метрик и `GetLatestSnapshot()` от службы угроз (тяжёлая работа не на UI-потоке).

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

## UI-паттерн (V2+, актуален для колец метрик)

```
TrayAppContext / MainForm
        │
        ▼
OverlayPanelV*  ──► RingIndicatorControl × N
        │              hover/click → MetricDetailPopup
        ├── PanelColors (геометрия, палитра)
        └── SystemMetricsProvider (1 с)
```

Расширение новой метрики: добавить `RingIndicatorControl` в designer, подключить источник в тикере панели.

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
| Прилипание к краю при перетаскивании | ✅ V6 |
| Метрика сети | ✅ V4+ |
| CPU по ядрам в выноске | ✅ V4+ |
| Кто подключился к АРМ | ✅ V5/V6 — [THREAT_MONITOR.md](./THREAT_MONITOR.md) |
| Таблица подключений / пакеты | ✅ V6 — [CONNECTIONS.md](./CONNECTIONS.md) |
