# Connections (V6)

Окно **Connections** — список сетевых подключений процессов (TCP/UDP, состояния, endpoints, PID), фильтры и детальная панель. Для просмотра **пакетов** (HEX + ASCII) нужен драйвер **Npcap**.

Открытие: из интерфейса V6 (меню/команда, связанная с `ConnectionsForm`).

## Npcap

SidePanelApp **не включает** Npcap в exe. Установщик распространяется отдельно.

- Скачать: [npcap.com](https://npcap.com/#download) (в проекте ориентир — `npcap-1.89.exe`).
- Условия redistribution: [OEM / license](https://npcap.com/oem/).

### Для разработки и сборки

1. Положить установщик в репозиторий (не коммитится, если в `.gitignore` только `*.exe` — файл всё равно локально):

   `SidePanelApp/tools/npcap-1.89.exe`

2. MSBuild копирует его в выход рядом с приложением:

   `SidePanelApp.V6/bin/<Config>/Installer/npcap-1.89.exe`

3. В окне Connections появится **«Установить Npcap (локально)»**, если файл есть в `Installer`.

После установки Npcap перезапустите SidePanelApp.V6.

Краткая копия этой памятки лежит в `SidePanelApp.V6/Installer/README.txt` (попадает в папку сборки).

## Код

Каталог `SidePanelApp.V6/Connections/` — форма, захват TCP payload, интеграция с Npcap (`NpcapAvailability`, `NpcapInstallHelper`).
