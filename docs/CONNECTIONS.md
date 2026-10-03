# Connections (V6)

Окно **Connections** (`ConnectionsForm`) — список сетевых подключений процессов: TCP/UDP, состояние, endpoints, PID, процесс. Открытие: меню трея **«Соединения (TCP/UDP)…»**.

## Окно

- Фильтры: все, установленные, listening, UDP, локальные.
- Автообновление 1 / 2 / 5 с или выкл. (`ConnectionsRefreshSeconds`); кнопка обновления сейчас. Пока курсор над таблицей таймер стоит.
- Строки, которые только что пропали из таблицы, ещё коротко видны как недавно закрытые.
- Детали выбранной строки: процесс (иконка, путь, метаданные файла), сворачиваемые секции.
- Контекстное меню строки и кнопки деталей:
  - **Закрыть соединение** — активный TCP (`TcpConnectionCloser`). Если Win32 отвечает отказом в доступе, предлагается перезапуск от администратора (`AdminElevationHelper`).
  - **Пропинговать** / **Traceroute** — вывод в секции ping/trace, текст хранится по ключу соединения.
  - **Показать пакеты** — только TCP. Список в стиле Wireshark (номер, время, направление, длина) и HEX + ASCII. Захват payload идёт через Npcap; без драйвера секция пакетов недоступна.

Код: `SidePanelApp.V6/Connections/` (форма, таблица, пакеты, ping/trace) и `Network/` (чтение TCP/UDP-таблиц, обогащение, закрытие TCP).

## Npcap

Классы: `Connections/NpcapAvailability`, `Infrastructure/NpcapInstallHelper`.

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
