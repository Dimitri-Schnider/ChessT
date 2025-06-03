# ChessClient

Dieses Projekt ist die Frontend-Anwendung des Schachspiels, realisiert als Blazor WebAssembly Single-Page Application (SPA). Es stellt die grafische Benutzeroberfläche (GUI) bereit, über die Benutzer mit dem Spiel interagieren können. Dies umfasst das Erstellen von Spielen, das Beitreten zu existierenden Partien, das Ausführen von Zügen auf dem Schachbrett und die Interaktion mit dem Kartenspiel-Aspekt. Die Kommunikation mit dem `ChessServer` erfolgt über HTTP-Anfragen (mittels einer Implementierung von `IGameSession`) für Aktionen und über eine SignalR-Verbindung (mittels `ChessHubService`) für den Empfang von Echtzeit-Aktualisierungen.

---
## Architektur und Dateistruktur

Die Struktur des `ChessClient`-Projekts ist typisch für eine Blazor WebAssembly-Anwendung:

* **`Pages/` Verzeichnis**: Enthält die Haupt-Razor-Komponenten, die als navigierbare Seiten der Anwendung dienen.
    * **`Chess.razor`**: Die zentrale Komponente für das eigentliche Schachspiel. Sie ist verantwortlich für die Anzeige des Schachbretts, der Timer, der Handkarten, des Karteninfo-Panels und des Verlaufs gespielter Karten. Sie handhabt die Interaktion mit dem Benutzer (Züge, Kartenauswahl), verwaltet den Spielzustand auf Client-Seite und kommuniziert mit den Backend-Diensten (`IGameSession`, `ChessHubService`).
    * **`Chess.razor.cs`**: Die Code-Behind-Datei für `Chess.razor`, die einen Grossteil der C#-Logik für die Spielseite enthält.
    * **`Chess.razor.Cards.cs`**: Eine partielle Klasse für `Chess.razor`, die speziell die Logik für die Handhabung und Aktivierung von Spielkarten kapselt.
    * **`Pages/Components/` Unterverzeichnis**: Beinhaltet wiederverwendbare UI-Komponenten, die auf den Seiten eingesetzt werden:
        * `ChessBoard.razor`: Stellt das visuelle Schachbrett dar, ermöglicht Figurenbewegungen per Klick oder Drag-and-Drop und hebt legale Züge oder Felder für Kartenaktionen hervor.
        * `SquareComponent.razor`: Repräsentiert ein einzelnes Feld auf dem Schachbrett, zeigt Figuren an und handhabt Interaktionen auf Feldebene.
        * `TimersDisplay.razor`: Zeigt die Bedenkzeiten der beiden Spieler an.
        * `InfoBox.razor`: Eine Komponente zur Anzeige von temporären Informationsmeldungen für den Benutzer.
        * `EndGamePopup.razor`: Ein Modal-Dialog, der bei Spielende angezeigt wird und eine Option für ein neues Spiel bietet.
        * Modals (`CreateGameModal.razor`, `JoinGameModal.razor`, `InviteLinkModal.razor`, `PawnPromotionModal.razor`): Dialoge für verschiedene Benutzerinteraktionen wie Spielerstellung, Spielbeitritt, Teilen eines Einladungslinks und Bauernumwandlung.
        * `LogsPanel.razor`: Zeigt (primär für Entwicklungszwecke) ein Protokoll der API-Aufrufe an.
        * `Pages/Components/Cards/` Unterverzeichnis: Komponenten spezifisch für die Kartenmechanik:
            * `CardInfoPanel.razor`: Zeigt detaillierte Informationen zu einer ausgewählten Karte an und bietet Optionen zur Aktivierung.
            * `DrawPilesDisplay.razor`: Visualisiert den Nachziehstapel des Spielers.
            * `HandCardsDisplay.razor`: Stellt die Karten dar, die der Spieler aktuell auf der Hand hält.
            * `PlayedCardsHistoryDisplay.razor`: Zeigt eine Historie der von beiden Spielern gespielten Karten an.

---
* **`Layout/` Verzeichnis**: Definiert die Struktur und das Layout der Anwendung.
    * **`MainLayout.razor`**: Das Hauptlayout, das typischerweise Navigationsmenü, Seiteninhalt (`@Body`) und ggf. Fusszeilen enthält. Es verwaltet auch den `RightSideDrawer`.
    * **`NavMenu.razor`**: Die Navigationsleiste der Anwendung, die Links zum Erstellen oder Beitreten von Spielen enthält.
    * **`RightSideDrawer.razor`**: Eine ausfahrbare Seitenleiste für Einstellungen und das Log-Panel.

---
* **`Services/` Verzeichnis**: Enthält clientseitige Dienste.
    * **`ChessHubService.cs`**: Verwaltet die SignalR-Verbindung zum `ChessHub` auf dem Server, sendet Nachrichten und empfängt Echtzeit-Updates vom Server.
    * **`ModalService.cs`**: Ein Dienst, der es Komponenten ermöglicht, das Anzeigen von globalen Modal-Dialogen (wie `CreateGameModal` und `JoinGameModal`) anzufordern.
    * **`LoggingService.cs`** und **`LoggingHandler.cs`**: Implementieren ein clientseitiges Logging-System, insbesondere für HTTP-Anfragen und -Antworten, die für Debugging-Zwecke im `LogsPanel` angezeigt werden können.
    * **`CardService.cs`**: Verwaltet die Definitionen der Spielkarten, den Nachziehstapel des Clients und die Logik zum Ziehen von Karten für die Hand des Spielers.

---
* **`Models/` Verzeichnis**: Enthält clientseitige Datenmodelle und Parameterobjekte.
    * **`CardDto.cs`**: Repräsentiert eine Spielkarte auf dem Client mit Eigenschaften wie Name, Beschreibung und Bild-URL.
    * **`CreateGameParameters.cs`**, **`JoinGameParameters.cs`**: DTOs, die Parameter für die Erstellung bzw. den Beitritt zu einem Spiel über die Modals kapseln.
    * **`PlayedCardInfo.cs`**: Speichert Informationen über eine bereits gespielte Karte zur Anzeige im Client-Verlauf.

---
* **`Configuration/` Verzeichnis**:
    * **`ClientConstants.cs`**: Definiert clientseitige Konstanten, insbesondere die Basis-URL des Servers (`DefaultServerBaseUrl`) und den relativen Pfad zum SignalR-Hub (`ChessHubRelativePath`).

---
* **`Extensions/` Verzeichnis**:
    * **`PieceDtoExtensions.cs`**: Enthält Erweiterungsmethoden für das `PieceDto` (aus `ChessNetwork`), um beispielsweise den Pfad zum entsprechenden Figurenbild zu erhalten.

---
* **`Utils/` Verzeichnis**:
    * **`PositionHelper.cs`**: Eine Hilfsklasse zur Konvertierung zwischen algebraischer Schachnotation und internen Zeilen-/Spaltenindizes.

---
* **`wwwroot/` Verzeichnis**: Enthält statische Dateien.
    * **`css/app.css`**: Globale CSS-Stile für die Anwendung.
    * **`js/`**: JavaScript-Dateien für Interop-Funktionalität (z.B. `CopyToClipboard.js` für das Kopieren in die Zwischenablage, `layoutInterop.js` für Layout-Anpassungen, `chessDnD.js` für Drag-and-Drop auf dem Schachbrett).
    * **`img/`**: Bilddateien für Schachfiguren, das Schachbrett, Karten-Templates und Karten-Artworks.
    * **`index.html`**: Die Haupt-HTML-Datei, die die Blazor-Anwendung hostet.
    * **`favicon.png`**: Das Favicon der Anwendung.

---
* **`Program.cs`**: Der Haupteinstiegspunkt der Blazor WebAssembly-Anwendung. Hier werden grundlegende Dienste konfiguriert und registriert, wie der `HttpClient` (konfiguriert mit `HttpGameSession` für die Serverkommunikation und `LoggingHandler`), der `ChessHubService`, `ModalService` und `CardService`.

---
* **`App.razor`**: Die Root-Komponente der Blazor-Anwendung, die das Routing-Setup enthält.

---
* **`_Imports.razor`**: Definiert gemeinsame `@using`-Anweisungen für alle Razor-Komponenten im Projekt.

---
* **`ChessClient.csproj`**: Die Projektdatei, die Abhängigkeiten wie `Microsoft.AspNetCore.Components.WebAssembly`, `Microsoft.AspNetCore.SignalR.Client` und den Projektverweis auf `ChessNetwork` definiert.

Der Client ist darauf ausgelegt, eine interaktive und reaktionsschnelle Benutzererfahrung zu bieten, indem er die Vorteile von Blazor WebAssembly für die UI-Logik und SignalR für Echtzeit-Updates nutzt.