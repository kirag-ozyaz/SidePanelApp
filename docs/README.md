# Документация SidePanelApp

## Обязательное

- **[ARCHITECTURE.md](./ARCHITECTURE.md)** — проекты solution, V6 (трей, панель, прилипание к краю, клюв попапа), сборка, иконка `app.ico`.

## По функциям

- **[THREAT_MONITOR.md](./THREAT_MONITOR.md)** — индикатор «внимание к АРМ» (RDP/VNC/AnyDesk, SMB, 4625): модель, каналы, настройки, ограничения. Реализовано в V5/V6; в V6 опрос вынесен в фон (`ThreatBackgroundService`).

- **[CONNECTIONS.md](./CONNECTIONS.md)** — окно **Connections** в V6: таблица TCP/UDP, закрытие соединения, ping/traceroute, пакеты, Npcap.

## Вне docs

- `SidePanelApp.V6/Installer/README.txt` — краткая памятка по Npcap рядом с установщиком в выходной папке сборки.
