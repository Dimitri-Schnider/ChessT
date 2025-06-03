# ChessServer Projekt

## Übersicht

`ChessServer` ist die Backend-Komponente der SchachT-Applikation. Es handelt sich um eine ASP.NET Core Web API, die für die Verwaltung von Schachpartien, die Durchsetzung der Spiellogik und die Echtzeitkommunikation mit den Clients zuständig ist.

## Hauptverantwortlichkeiten

* **API-Endpunkte**: Stellt eine RESTful API bereit (definiert in `GamesController.cs`) für Operationen wie:
    * Erstellen neuer Spiele.
    * Beitreten zu bestehenden Spielen.
    * Empfangen und Verarbeiten von Spielerzügen.
    * Aktivieren von Spezialkarten.
    * Abrufen von Spielzuständen (Brett, Status, Zeit).
    * Herunterladen des Spielverlaufs.
* **Spielverwaltung**: Nutzt den `IGameManager` (implementiert durch `InMemoryGameManager.cs`), um Instanzen von `GameSession.cs` zu erstellen und zu verwalten. Jede `GameSession` repräsentiert eine laufende Schachpartie.
* **Spiellogik-Orchestrierung**: Die `GameSession`-Klasse orchestriert die Kernlogik aus dem `ChessLogic`-Projekt, einschliesslich Zugvalidierung, Zustandsaktualisierungen und Endebedingungen.
* **Karteneffekte**: Die Logik für Spezialkarten wird über ein Strategy-Pattern (Interface `ICardEffect.cs` und konkrete Implementierungen im Verzeichnis `Services/CardEffects/`) innerhalb der `GameSession` flexibel gehandhabt.
* **Zeitmanagement**: Der `GameTimerService.cs` ist verantwortlich für die Verwaltung der Bedenkzeiten der Spieler und die Erkennung von Zeitüberschreitungen.
* **Echtzeitkommunikation**: Verwendet SignalR (`ChessHub.cs`), um Spielaktualisierungen (Züge, Zeit, Spielende, Kartenaktivierungen) in Echtzeit an die verbundenen Clients (`ChessClient`) zu senden.

## Projektstruktur (relevante Teile für ChessServer)

    ChessServer/
    ├── Controllers/
    │   └── GamesController.cs      # API-Endpunkte für Spielinteraktionen
    ├── Hubs/
    │   └── ChessHub.cs             # SignalR-Hub für Echtzeitkommunikation
    ├── Services/
    │   ├── IGameManager.cs
    │   ├── InMemoryGameManager.cs  # Implementierung zur Verwaltung von Spielsitzungen
    │   ├── GameSession.cs          # Logik und Zustand einer einzelnen Partie
    │   ├── GameTimerService.cs     # Zeitmanagement für Partien
    │   ├── PieceExtensions.cs      # Hilfsmethoden für Figuren-DTOs
    │   └── CardEffects/            # Verzeichnis für Karteneffekt-Strategien
    │       ├── ICardEffect.cs
    │       ├── ExtraZugEffect.cs
    │       ├── TeleportEffect.cs
    │       └── ... (weitere Effektklassen)
    ├── Configuration/
    │   └── ServerConstants.cs      # Server-spezifische Konstanten (z.B. Hub-Routen)
    ├── Properties/
    │   └── launchSettings.json     # Konfiguration für den Start des Servers (Ports etc.)
    ├── ChessServer.csproj          # Projektdatei mit Abhängigkeiten
    └── Program.cs                  # Konfiguration und Start des Webservers

## Verwendete Technologien

* **.NET 9 / ASP.NET Core**: Basis-Framework für die Web API und den Server.
* **SignalR**: Für die Echtzeitkommunikation mit den Clients.
* **RESTful API Design**: Für die Interaktion mit dem `ChessClient`.
* **C#**: Programmiersprache.

## Einrichtung und Start

1.  **Voraussetzungen**:
    * .NET 9 SDK.
    * Ein Code-Editor wie Visual Studio oder JetBrains Rider.
2.  **Build**:
    * Navigieren Sie in das Hauptverzeichnis der Solution.
    * Führen Sie `dotnet build Chess.sln` aus.
3.  **Starten**:
    * Navigieren Sie in das `ChessServer`-Verzeichnis.
    * Führen Sie `dotnet run` aus.
    * Der Server startet und lauscht auf den in `Properties/launchSettings.json` konfigurierten Ports (standardmässig oft `https://localhost:7144` und `http://localhost:xxxx`).

## API-Dokumentation

Wenn der Server in der Entwicklungsumgebung (`IsDevelopment()`) gestartet wird, ist Swagger UI unter dem Pfad `/swagger` verfügbar, um die API-Endpunkte zu testen und zu dokumentieren.