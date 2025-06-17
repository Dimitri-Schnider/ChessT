# ChessClient

## Übersicht

Dieses Projekt ist die Frontend-Anwendung des Schachspiels, realisiert als Blazor WebAssembly Single-Page Application (SPA). Es stellt die grafische Benutzeroberfläche (GUI) bereit, über die Benutzer mit dem Spiel interagieren. Dies umfasst das Erstellen und Beitreten von Partien, das Ausführen von Zügen und die Interaktion mit dem zentralen Twist des Spiels: einem strategischen Kartensystem.

Die Kommunikation mit dem `ChessServer` erfolgt über zwei Kanäle:
* **HTTP-Anfragen:** Für Aktionen wie das Erstellen eines Spiels oder das Senden eines Zugs. Dies wird über eine Implementierung von `IGameSession` abgewickelt.
* **SignalR:** Für den Empfang von Echtzeit-Aktualisierungen des Spielzustands (z.B. Züge des Gegners, Zeit-Updates, gespielte Karten).

## Kernarchitektur

Die Client-Architektur basiert auf einem reaktiven, zustands-gesteuerten Muster, das die UI-Logik klar von der Geschäftslogik und dem Daten-Handling trennt.

### 1. Zustands-Management (State Management)

Das Herzstück der Architektur ist ein Satz von zentralisierten State-Containern. Jeder Container ist für einen bestimmten Aspekt des Anwendungszustands verantwortlich und benachrichtigt die UI-Komponenten über Änderungen mittels eines `StateChanged`-Events.

* **`IGameCoreState`**: Verwaltet den fundamentalen Spielzustand: Brettstellung, wer am Zug ist, Spielerinformationen, Timer-Zeiten und den Spielstatus (laufend, beendet, etc.).
* **`ICardState`**: Verwaltet alles rund um die Spielkarten: Handkarten, Nachziehstapel, gespielte Karten und den komplexen Zustand während der Aktivierung einer Karte.
* **`IModalState`**: Steuert die Sichtbarkeit und die Daten aller Modal-Dialoge (z.B. "Spiel erstellen", "Figurenauswahl", "Fehlermeldung") und stellt sicher, dass immer nur ein Modal aktiv ist.
* **`IUiState`**: Verwaltet temporäre und globale UI-Zustände wie Lade-Overlays, Countdowns, Info-Box-Nachrichten und die globalen Sieg/Niederlage-Animationen.
* **`IHighlightState`**: Verwaltet alle visuellen Hervorhebungen auf dem Schachbrett, sei es für legale Züge, den letzten Zug des Gegners oder komplexe Karteneffekte.
* **`IAnimationState`**: Steuert den Zustand für bildschirmfüllende Animationen, insbesondere die Aktivierung und den Tausch von Karten.

### 2. Service-Schicht (Service Layer)

Dienste kapseln die Geschäftslogik und die Kommunikationsabläufe.

* **`GameOrchestrationService`**: Der zentrale "Dirigent" der Anwendung. Er koordiniert die Abläufe zwischen UI-Interaktionen, State-Updates und Server-Anfragen. Wenn ein Spieler beispielsweise eine Karte aktiviert, steuert dieser Dienst den gesamten mehrstufigen Prozess.
* **`HubSubscriptionService`**: Dient als Brücke zwischen der rohen SignalR-Verbindung (`ChessHubService`) und den State-Containern. Er abonniert die Hub-Events und übersetzt die vom Server empfangenen Daten in konkrete Zustands-Änderungen (z.B. "aktualisiere das Brett", "füge eine Karte zur Hand hinzu").
* **`ChessHubService`**: Verwaltet die Low-Level-Verbindung zum SignalR-Hub, sendet Nachrichten und empfängt die Echtzeit-Updates vom Server.
* **`ModalService`** & **`TourService`**: Einfache Dienste, die es Komponenten ermöglichen, globale Aktionen wie das Anzeigen eines Modals oder den Start der interaktiven Tour anzufordern.

## Dateistruktur & Komponenten

Die Struktur ist typisch für eine Blazor WebAssembly-Anwendung, mit einem starken Fokus auf komponentenbasierte Entwicklung.

* **`Pages/`**:
    * `Chess.razor`: Die zentrale Komponente und Hauptseite. Sie instanziiert und orchestriert fast alle anderen UI-Komponenten und verbindet die UI mit den Diensten und Zustands-Containern.

* **`Pages/Components/`**: Wiederverwendbare UI-Komponenten.
    * **Spielbrett**:
        * `ChessBoard.razor`: Stellt das visuelle Schachbrett dar, ermöglicht Züge per Klick oder Drag-and-Drop und zeigt die von `IHighlightState` vorgegebenen Hervorhebungen an.
        * `SquareComponent.razor`: Repräsentiert ein einzelnes Feld und ist die grundlegendste visuelle Einheit des Bretts.
    * **Karten**:
        * `HandCardsDisplay.razor`: Zeigt die Handkarten des Spielers in einer scrollbaren Ansicht mit 3D-Flip-Animation.
        * `CardInfoPanel.razor`: Zeigt eine vergrösserte Detailansicht einer Karte zur Vorschau oder Aktivierung.
        * `PlayedCardsHistoryDisplay.razor`: Zeigt die Historie der von beiden Spielern gespielten Karten an.
        * `DrawPilesDisplay.razor`: Visualisiert den Nachziehstapel.
    * **Animationen**:
        * `CardActivationAnimation.razor`: Eine bildschirmfüllende Animation, die beim Spielen einer Karte angezeigt wird.
        * `CardSwapSpecificAnimation.razor`: Eine spezielle Animation für den Kartentausch-Effekt.
        * `LandingAnimation.razor`: Die Startanimation der App.
        * `GlobalEffectsOverlay.razor`: Verantwortlich für globale Sieg- (Konfetti) und Niederlage-Animationen.
    * **Modals & UI-Elemente**:
        * `CreateGameModal.razor`, `JoinGameModal.razor`, `InviteLinkModal.razor`: Dialoge für das Erstellen, Beitreten und Teilen von Spielen.
        * `PieceSelectionModal.razor`: Ein wiederverwendbarer Dialog zur Figurenauswahl, der sowohl für die Bauernumwandlung als auch für den "Wiedergeburt"-Karteneffekt genutzt wird.
        * `WinLossModal.razor`: Ein aufwändig gestalteter Dialog, der am Spielende angezeigt wird und mehrere Aktionen anbietet.
        * `ErrorModal.razor`: Ein standardisierter Dialog zur Anzeige von Fehlermeldungen.
        * `InfoBox.razor`: Zeigt temporäre, nicht-blockierende Benachrichtigungen an.
        * `TimersDisplay.razor`: Zeigt die Bedenkzeiten der Spieler an.

* **`Layout/`**: Definiert das globale Anwendungs-Layout.
    * `MainLayout.razor`: Das Hauptlayout, das Navigationsmenü und Seiteninhalt einbettet.
    * `NavMenu.razor`: Die seitliche Navigationsleiste.
    * `RightSideDrawer.razor`: Eine ausfahrbare Seitenleiste für Einstellungen und die API-Logs.

* **`wwwroot/`**: Enthält statische Dateien.
    * **`js/`**: JavaScript-Dateien für erweiterte Interaktivität:
        * `chessDnD.js`: Implementiert die Drag-and-Drop-Logik für die Schachfiguren mittels `interact.js`.
        * `layoutInterop.js`: Hilft bei der Erkennung der Viewport-Grösse für das responsive Layout.
        * `handCardsDisplayInterop.js`: Steuert das programmgesteuerte Scrollen der Handkarten.
        * `navigationInterop.js`: Implementiert die "Seite verlassen?"-Warnung.
        * `tourInterop.js`: Steuert die interaktive Tour mit `driver.js`.
        * `CopyToClipboard.js`: Kapselt die Logik zum Kopieren von Text in die Zwischenablage.
    * **`css/`**: Globale und komponenten-spezifische Stylesheets.
    * **`img/`**: Alle Bilddateien für Figuren, Karten und UI-Elemente.

* **`Program.cs`**: Der Haupteinstiegspunkt der Anwendung. Hier werden alle Dienste und State-Container für die Dependency Injection konfiguriert und registriert.