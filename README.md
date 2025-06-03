// Diese Datei enthält die Projektbeschreibung, eine Architekturübersicht,
// Informationen zu Technologien, Projektstruktur, Einrichtung, Build und Start.
# SchachT

Dieses Repository beinhaltet den Quellcode für eine webbasierte Schachapplikation. Es implementiert eine Client-Server-Architektur unter Verwendung moderner .NET-Technologien.

## Architekturübersicht

Die Anwendung ist in vier Hauptprojekte unterteilt, die klar definierte Aufgabenbereiche abdecken:

* **ChessLogic**: Dieses Projekt bildet das Fundament der Schachlogik. Es beinhaltet die Implementierung des Schachbretts, der verschiedenen Figuren mit ihren spezifischen Bewegungsregeln, die Validierung von Zügen, die Verwaltung des Spielzustands (z.B. welcher Spieler am Zug ist, Erkennung von Schach oder Matt) und die Logik zur Bestimmung des Spielendes unter verschiedenen Bedingungen (Schachmatt, Patt, 50-Züge-Regel etc.).
* **ChessNetwork**: Verantwortlich für die Definition der Kommunikationsschnittstellen und Datenmodelle zwischen Client und Server. Dies umfasst Data Transfer Objects (DTOs) für Spielzustände, Züge und Spielerinformationen sowie das `IGameSession`-Interface, das die API-Verträge für Spielinteraktionen festlegt. Eine konkrete Implementierung für HTTP-basierte Kommunikation (`HttpGameSession`) ist ebenfalls Teil dieses Projekts.
* **ChessServer**: Dient als Backend der Anwendung und ist als ASP.NET Core Web API realisiert. Der Server übernimmt die Verwaltung von Spielsitzungen (Erstellung, Beitritt), die serverseitige Durchsetzung der Spiellogik durch den `IGameManager`-Dienst und die Echtzeitkommunikation mit den Clients über einen SignalR Hub (`ChessHub`).
* **ChessClient**: Eine Single-Page Application (SPA), entwickelt mit Blazor WebAssembly, die die grafische Benutzeroberfläche des Schachspiels darstellt. Benutzer interagieren über diese Oberfläche, um Spiele zu erstellen, beizutreten und Züge auszuführen. Der Client kommuniziert mit dem `ChessServer` über HTTP-Anfragen (definiert durch `IGameSession`) und empfängt Echtzeit-Aktualisierungen (z.B. Züge des Gegners, Zeit-Updates) mittels des `ChessHubService`, der die SignalR-Clientverbindung verwaltet.
## Verwendete Technologien

* **Programmiersprache**: C#
* **Frameworks**:
    * .NET 9
    * ASP.NET Core (für `ChessServer` zur Bereitstellung der Web API und SignalR-Hubs)
    * Blazor WebAssembly (für `ChessClient` zur Erstellung der clientseitigen Benutzeroberfläche)
* **Echtzeitkommunikation**: SignalR (sowohl serverseitig im `ChessServer` als auch clientseitig im `ChessClient`)
* **API-Design**: RESTful API-Endpunkte im `ChessServer`.
## Projektstruktur

Die Projektmappe (`Chess.sln`) bündelt die einzelnen Projekte. Editor-spezifische Konfigurationen und Analyse-Einstellungen sind über `.editorconfig` und `Directory.Build.props` global festgelegt.
~~~
/
├── .editorconfig # Editor-Konfigurationen und Code-Style-Regeln
├── Chess.sln # Haupt-Projektmappendatei
├── Directory.Build.props # Globale MSBuild-Eigenschaften für alle Projekte
├── README.md # Diese Datei
├── ChessClient/ # Blazor WebAssembly Client-Anwendung
│ ├── wwwroot/ # Statische Client-Assets (CSS, JS, Bilder, Bibliotheken)
│ ├── Pages/ # Razor-Komponenten, die UI-Seiten definieren (z.B. Chess.razor)
│ ├── Layout/ # Layout-Komponenten (z.B. MainLayout, NavMenu)
│ ├── Services/ # Client-spezifische Dienste (z.B. ChessHubService, ModalService)
│ ├── Models/ # Client-seitige Datenmodelle (z.B. CardDto, CreateGameParameters)
│ └── ChessClient.csproj
├── ChessLogic/ # Bibliothek mit der Kernlogik des Schachspiels
│ ├── Pieces/ # Definitionen und Logik der Schachfiguren (z.B. King, Pawn)
│ ├── Moves/ # Definitionen und Logik verschiedener Zugtypen (z.B. Castle, NormalMove)
│ ├── Utilities/ # Hilfsklassen (z.B. Position, Direction)
│ └── ChessLogic.csproj
├── ChessNetwork/ # Bibliothek für Netzwerkkommunikation und DTOs
│ ├── DTOs/ # Data Transfer Objects für die API (z.B. BoardDto, MoveDto)
│ └── ChessNetwork.csproj
└── ChessServer/ # ASP.NET Core Server-Anwendung
    ├── Controllers/ # API-Controller zur Verarbeitung von HTTP-Anfragen (z.B. GamesController)
    ├── Hubs/ # SignalR Hubs für Echtzeitkommunikation (z.B. ChessHub)
    ├── Services/ # Server-seitige Dienste (z.B. IGameManager, InMemoryGameManager)
    └── ChessServer.csproj
~~~
## Einrichtung

1. **Voraussetzungen**:
    * .NET 9 SDK (oder die im Projekt verwendete Framework-Version).
    * Ein Code-Editor wie Visual Studio 2022+ oder JetBrains Rider.
2. **Abhängigkeiten**:
    * Die Projektmappe `Chess.sln` referenziert die einzelnen Projekte. Stellen Sie sicher, dass alle NuGet-Pakete wiederhergestellt sind (üblicherweise automatisch durch die IDE beim Öffnen der Solution).
3. **Konfiguration**:
    * Die Server-URL, mit der sich der `ChessClient` verbindet, ist in `ChessClient/Program.cs` (für API-Aufrufe) und `ChessClient/Pages/Chess.razor` (für SignalR) konfiguriert. Standardmässig wird `https://localhost:7144` verwendet.
    * Der `ChessServer` ist so konfiguriert, dass er auf den in `ChessServer/Properties/launchSettings.json` definierten Ports lauscht.
## Build

Die Anwendung kann über die .NET CLI oder eine IDE (Visual Studio, Rider) gebaut werden:

* **Über die Kommandozeile**:
    dotnet build Chess.sln

* **Über die IDE**: Öffnen Sie `Chess.sln` und nutzen Sie die Build-Funktion der IDE.
## Starten der Anwendung

1. **Server starten**:
    * Navigieren Sie in das `ChessServer`-Verzeichnis oder wählen Sie es in Ihrer IDE als Startprojekt aus.
    * Führen Sie aus:
        dotnet run
    * Der Server startet und lauscht auf den konfigurierten Ports (z.B. `https://localhost:7144`).
2. **Client starten**:
    * Navigieren Sie in das `ChessClient`-Verzeichnis oder wählen Sie es in Ihrer IDE als Startprojekt aus.
    * Führen Sie aus:
        dotnet run
    * Die Blazor WebAssembly-Anwendung wird gebaut und üblicherweise in einem neuen Browserfenster geöffnet. Stellen Sie sicher, dass der Server läuft, bevor der Client gestartet wird, da der Client versucht, eine Verbindung zum Server herzustellen.