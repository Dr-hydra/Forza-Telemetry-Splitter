# Forza Telemetry Splitter

[English](README.md) · [日本語](README.ja.md) · [Français](README.fr.md) · **Deutsch** · [Español](README.es.md)

Sende die Telemetrie von Forza an mehrere Tools gleichzeitig.

Die „Data Out"-Telemetrie von Forza Horizon 6 kann nur an eine IP-Adresse und einen Port gesendet
werden. Das zwingt zur Wahl: entweder [VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu)
(automatisches Schalten), ein Tuning-Tool oder ein Dashboard — aber nicht alle zusammen.

Forza Telemetry Splitter setzt sich dazwischen. Er empfängt die Telemetrie von Forza auf einem eigenen
Port und sendet jedes Paket unverändert an beliebig viele lokale Tools weiter. Der zusätzliche Aufwand
liegt unter einer Millisekunde und die Daten werden nicht verändert — jedes Tool verhält sich genau so,
als würde es direkt mit Forza kommunizieren.

Nicht verbunden mit oder unterstützt von Turn 10, Playground Games oder Microsoft. „Forza" ist eine
Marke von Microsoft.

## Funktionen

| Funktion | Beschreibung |
|----------|--------------|
| Verteilung | Verteilt die Forza-Telemetrie an beliebig viele Ziele, Pakete unverändert. |
| Mehrere Spiele | Funktioniert mit Forza Horizon 4/5/6 und Forza Motorsport (7, 2023). Das Spiel wird automatisch erkannt. |
| Status-Overlay | Eine kleine Anzeige oben rechts zeigt „Verbunden / Keine Daten" sowie Gang und Tempo in Echtzeit. |
| Mehrsprachig | Englisch, Japanisch, Französisch, Deutsch, Spanisch. Automatisch nach der Windows-Sprache. |
| Tray-App | Läuft unauffällig im Infobereich, wie VirtualTCU. |
| Kein Administrator nötig | Nur lokales UDP — keine UAC-Abfrage. |

## Installation

Empfohlen — das Installationsprogramm:

1. Lade `ForzaTelemetrySplitterInstaller.exe` von der [Releases](../../releases)-Seite herunter.
2. Rechtsklick → Eigenschaften → unten im Reiter „Allgemein" „Zulassen" anhaken → OK. Das vermeidet den
   Bildschirm „Der Computer wurde durch Windows geschützt".
3. Ausführen. Installation pro Benutzer, also keine Administrator-Abfrage. Bietet eine Desktop-Verknüpfung
   und eine Option „Mit Windows automatisch starten".
4. Startet nach Abschluss im Infobereich.

Fortgeschritten / ohne Installation: Lade stattdessen `ftsPortable.exe` herunter und starte sie direkt.
Für die meisten wird das Installationsprogramm oben empfohlen.

## Einrichtung im Spiel

1. Öffne die App aus dem Infobereich. Sie wartet auf Port **44405** und ist bereits eingestellt, an
   VirtualTCU (dessen normalen Port 5555) weiterzuleiten.
2. Öffne in deinem Forza-Spiel „Data Out" (bei Horizon: Einstellungen → HUD und Gameplay → Data Out):
   - Data Out: an
   - IP-Adresse: `127.0.0.1`
   - Port: **`44405`**
   - Paketformat: Car Dash (Horizon) oder Dash (Motorsport)
3. Lass deine anderen Tools unverändert — der Splitter leitet an jedes auf seinem normalen Port weiter.
   Um ein Tool hinzuzufügen, klicke in der App auf „Hinzufügen".
4. Fahre los: Die Anzeige oben rechts wird grün und alle aktivierten Tools empfangen die Daten.

## Mehr (auf Englisch)

- [Aus dem Quellcode bauen](docs/BUILDING.md)
- [Windows-SmartScreen-Warnung](docs/SMARTSCREEN.md)
- [Fehler melden](docs/REPORTING-BUGS.md)
- [Mitwirken](CONTRIBUTING.md)
- [Lizenz (MIT)](LICENSE)

## Getestet auf

Windows 10 und 11. Forza Horizon 4/5/6 und Forza Motorsport (7, 2023) — automatisch erkannt.
