# ChessLogic

Dieses Projekt implementiert die Kernlogik des Schachspiels. Es ist verantwortlich für das Schachbrett, die Bewegungsregeln der Figuren, die Zugvalidierung und die Verwaltung des Spielzustands, einschliesslich der Erkennung von Schach, Matt und anderen Spielende-Bedingungen.

## Kernkomponenten

Das Projekt `ChessLogic` gliedert sich in folgende Hauptbereiche:

### 1. `Board.cs`
Das Herzstück der Brettdarstellung.
* Verwaltet ein 8x8-Array zur Positionierung der Schachfiguren.
* Speichert Informationen zu speziellen Zugmöglichkeiten wie En Passant (`pawnSkipPositions`).
* Bietet Methoden zur Überprüfung des Brettzustands (z.B. `IsEmpty`, `IsInside`).
* Initialisiert die Standard-Startaufstellung der Figuren.
* Unterstützt das Erstellen einer tiefen Kopie des Bretts (`Copy`).
* Ermittelt Rochaderechte und prüft auf die Möglichkeit von En-Passant-Schlägen.
* Beinhaltet Logik zur Erkennung von Remis durch unzureichendes Material (`InsufficientMaterial`).

### 2. `GameState.cs`
Verwaltet den dynamischen Zustand einer Schachpartie.
* Verfolgt, welcher Spieler aktuell am Zug ist (`CurrentPlayer`).
* Hält die aktuelle Brettkonfiguration (`Board`).
* Speichert das Spielergebnis (`Result`), sobald die Partie beendet ist.
* Implementiert die 50-Züge-Regel.
* Nutzt `StateString` zur Erkennung der dreifachen Stellungswiederholung.
* Stellt Methoden zur Verfügung, um alle legalen Züge zu ermitteln.
* Prüft nach jedem Zug auf Spielende-Bedingungen (`CheckForGameOver`).

### 3. `Pieces/` Verzeichnis
Enthält die Definitionen und die spezifische Logik für jede Schachfigur.
* **`Piece.cs` (Abstrakte Basisklasse)**: Definiert gemeinsame Eigenschaften aller Figuren (`Type`, `Color`, `HasMoved`) und Methoden (`Copy`, `GetMoves`).
* **Spezifische Figurenklassen** (`Bishop.cs`, `King.cs`, `Knight.cs`, `Pawn.cs`, `Queen.cs`, `Rook.cs`): Erben von `Piece` und implementieren die spezifischen Bewegungsregeln für den jeweiligen Figurentyp, z.B. Rochade im `King` oder En Passant im `Pawn`.

### 4. `Moves/` Verzeichnis
Definiert die verschiedenen Arten von Schachzügen und deren Ausführung.
* **`Move.cs` (Abstrakte Basisklasse)**: Definiert grundlegende Eigenschaften eines Zugs und die `Execute`-Methode.
* **Spezifische Zugklassen** (`NormalMove.cs`, `Castle.cs`, etc.): Implementieren die Logik für normale Züge, Rochaden, Bauernumwandlungen und Spezialzüge durch Karten (`TeleportMove.cs`, `PositionSwapMove.cs`).

## Hilfsklassen und Enumerationen
Diese Komponenten unterstützen die Kernlogik:

* **`Utilities/` Verzeichnis**: Enthält `Direction.cs` und `Position.cs` für die Positionsberechnung.
* **Enumerationen**: `EndReason.cs`, `MoveType.cs`, `PieceType.cs`, `Player.cs` definieren feste Werte für Spielzustände und -elemente.
* **Hilfsklassen**: `Counting.cs`, `Result.cs`, `StateString.cs` unterstützen Logiken wie die Materialzählung oder die FEN-Generierung.

## Designprinzipien
`ChessLogic` ist als eigenständiges Modul ohne Abhängigkeiten zu Benutzeroberflächen oder Netzwerktechnologien konzipiert. Dies gewährleistet eine hohe Wiederverwendbarkeit und Testbarkeit der Kernspiellogik.