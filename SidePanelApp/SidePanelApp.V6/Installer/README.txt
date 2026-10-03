Npcap для SidePanelApp.V6 (захват пакетов / HEX)
================================================

SidePanelApp не включает драйвер Npcap в exe. Для панели «Пакеты» (HEX + ASCII)
нужен Npcap.

Скачать установщик:
  https://npcap.com/dist/npcap-1.89.exe

Страница проекта:
  https://npcap.com/#download

Для разработки и сборки:
  Положите npcap-1.89.exe в репозиторий:
    tools\npcap-1.89.exe
  MSBuild копирует его в выход:
    Installer\npcap-1.89.exe
  (рядом с SidePanelApp.V6.exe)

В окне Connections появится «Установить Npcap (локально)», если файл есть в Installer.

Лицензия и условия redistribution — на https://npcap.com/oem/

После установки Npcap перезапустите SidePanelApp.V6.
