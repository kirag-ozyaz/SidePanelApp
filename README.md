# SidePanelApp

WinForms-приложение (.NET Framework 4.8.1): узкая панель у края экрана с метриками системы, мониторингом удалённого доступа к ПК и (в V6) окном сетевых подключений. Панель можно прилепить к левому или правому краю; выноски открываются с внутренней стороны.

**Актуальная версия для использования:** `SidePanelApp.V6` (трей, автозапуск, одна копия V1–V6 на машине).

## Структура репозитория

```
SidePanelApp/                 ← корень репозитория (этот README)
├── docs/                     ← документация
├── .gitignore
└── SidePanelApp/             ← решение Visual Studio
    ├── SidePanelApp.sln
    ├── Assets/app.ico
    ├── Directory.Build.targets
    ├── tools/                ← GenerateAppIcon (dev)
    ├── SidePanelApp/         ← V1, общий код (метрики, иконка)
    ├── SidePanelApp.V2 … V6/
    └── …
```

## Сборка и запуск

1. Открыть `SidePanelApp\SidePanelApp.sln` в Visual Studio 2022.
2. Установить стартовый проект **SidePanelApp.V6**.
3. Собрать **Debug** или **Release** (x64).

Из командной строки (путь к MSBuild может отличаться):

```powershell
cd X:\Project\SidePanelApp\SidePanelApp
& "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  SidePanelApp.V6\SidePanelApp.V6.csproj /p:Configuration=Debug
```

Готовый exe: `SidePanelApp.V6\bin\Debug\SidePanelApp.V6.exe` (или `Release`).

При первой сборке, если нет `Assets\app.ico` или утилиты генерации, MSBuild сам соберёт `tools\GenerateAppIcon` и создаст иконку (см. [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)).

## Документация

| Документ | Содержание |
|----------|------------|
| [docs/README.md](docs/README.md) | Оглавление |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Версии проектов, устройство V6, край панели и клюв попапа, иконка |
| [docs/THREAT_MONITOR.md](docs/THREAT_MONITOR.md) | Каналы «удалёнка / SMB / попытки входа», настройки |
| [docs/CONNECTIONS.md](docs/CONNECTIONS.md) | Окно Connections: таблица, ping/trace, закрытие TCP, Npcap |

## Версии в solution

| Проект | Кратко |
|--------|--------|
| SidePanelApp (V1) | Ранняя панель, общие библиотеки |
| V2 | Контролы колец, окно настроек |
| V3 | Запуск из трея |
| V4 | GPU, сеть, детальные попапы |
| V5 | Пятое кольцо — мониторинг доступа к АРМ |
| **V6** | Фон угроз, прилипание панели к краю, Connections (пакеты, ping, закрытие TCP) |

Старые V2–V5 оставлены для сравнения; в эксплуатации ориентируйтесь на V6.
