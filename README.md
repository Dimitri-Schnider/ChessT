# SchachT: Ein modernes Schachspiel mit Strategiekarten

Willkommen zum SchachT-Projekt! Dies ist eine moderne Web-Applikation, die das klassische Schachspiel mit einem innovativen System von Strategiekarten kombiniert. Spieler können nicht nur durch geschickte Züge, sondern auch durch den taktischen Einsatz von Karten, die das Spielgeschehen beeinflussen, gewinnen.

## Kernfeatures

* **Klassisches und erweitertes Gameplay**: Bietet die volle Schacherfahrung, erweitert durch ein einzigartiges System von Strategiekarten.
* **Spielmodi**: Unterstützt sowohl Spieler-gegen-Spieler (PvP) als auch Spieler-gegen-Computer (PvC) Partien mit verschiedenen Schwierigkeitsgraden.
* **Echtzeit-Interaktion**: Nutzt SignalR für eine verzögerungsfreie, reaktionsschnelle Kommunikation zwischen den Spielern und dem Server.
* **Bedenkzeit**: Integriertes Zeitmanagement für spannende Partien mit individuellen Uhren für jeden Spieler.
* **Moderne Web-Oberfläche**: Eine interaktive und responsive Single-Page Application (SPA), die mit Blazor WebAssembly entwickelt wurde.
* **Zentrales Logging**: Ein dediziertes Logging-Projekt sorgt für nachvollziehbare und strukturierte Protokolle über alle Systemkomponenten hinweg.
* **Separate Analyse-Anwendung**: Ein eigenständiges WPF-Tool zur Analyse von Spieldaten.

## Architekturübersicht

Die Anwendung ist in mehrere spezialisierte Projekte unterteilt, die klar definierte Aufgabenbereiche abdecken:

* **`ChessLogic`**: Das Herzstück der Anwendung. Dieses Projekt enthält die gesamte plattformunabhängige Spiellogik, inklusive der Brett-Repräsentation, Figurenregeln, Zugvalidierung und der Logik zur Bestimmung des Spielendes (Schachmatt, Patt, 50-Züge-Regel etc.).
* **`ChessNetwork`**: Definiert den Kommunikationsvertrag zwischen Client und Server. Es enthält alle Data Transfer Objects (DTOs) und die `IGameSession`-Schnittstelle.
* **`ChessServer`**: Der ASP.NET Core Backend-Server. Er stellt die RESTful API bereit, hostet den SignalR-Hub für Echtzeit-Updates und verwaltet die aktiven Spielsitzungen (`GameSession`) über den `InMemoryGameManager`.
* **`ChessClient`**: Die Blazor WebAssembly Frontend-Anwendung. Sie stellt die grafische Benutzeroberfläche (GUI) bereit, über die Benutzer mit dem Spiel interagieren, Züge ausführen und Karten spielen.
* **`Chess.Logging`**: Eine zentrale Bibliothek für das Logging. Sie wird sowohl vom Server als auch vom Client verwendet, um strukturierte und konsistente Logs zu erzeugen.
* **`ChessLogic.Tests`**: Enthält Unit-Tests für das `ChessLogic`-Projekt, um die Korrektheit der Spielregeln sicherzustellen.
* **`ChessAnalysis`**: Ein separates WPF-Desktop-Tool zur Analyse von Spieldaten, wahrscheinlich zur Auswertung von JSON-Logs oder Spielverläufen.

### Kommunikationsfluss

Die Anwendung folgt einer klassischen Client-Server-Architektur:
1.  Der **`ChessClient`** (Blazor Wasm) läuft vollständig im Browser des Benutzers.
2.  Für Spielaktionen wie das Erstellen eines Spiels oder das Ausführen eines Zugs sendet der Client **HTTP-Anfragen** an die API des **`ChessServer`**.
3.  Für Echtzeit-Aktualisierungen (z.B. Züge des Gegners, Zeit-Updates) baut der Client eine persistente **SignalR-Verbindung** zum `ChessHub` auf dem Server auf.
4.  Sowohl HTTP- als auch SignalR-Kommunikation nutzen die gemeinsamen Datenmodelle aus **`ChessNetwork`**.

## Verwendete Technologien

* **Programmiersprache**: C#
* **Frameworks**:
    * .NET 9
    * ASP.NET Core (für `ChessServer` zur Bereitstellung der Web API und SignalR-Hubs)
    * Blazor WebAssembly (für `ChessClient` zur Erstellung der clientseitigen Benutzeroberfläche)
    * WPF (für `ChessAnalysis`)
* **Echtzeitkommunikation**: SignalR
* **Testing**: xUnit

## Projektstruktur
~~~
/
├── .editorconfig
├── Chess.sln
├── Directory.Build.props
├── README.md
├── ChessClient/
│   ├── wwwroot/
│   ├── Pages/
│   ├── Layout/
│   ├── Services/
│   └── ChessClient.csproj
├── ChessLogic/
│   ├── Pieces/
│   ├── Moves/
│   └── ChessLogic.csproj
├── ChessNetwork/
│   ├── DTOs/
│   └── ChessNetwork.csproj
└── ChessServer/
├── Controllers/
├── Hubs/
├── Services/
└── ChessServer.csproj
├── Chess.Logging/
└── ChessLogic.Tests/
~~~