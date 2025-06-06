# ChessLogic

Dieses Projekt implementiert die Kernlogik des Schachspiels. Es ist verantwortlich für das Schachbrett, die Bewegungsregeln der Figuren, die Zugvalidierung und die Verwaltung des Spielzustands, einschliesslich der Erkennung von Schach, Matt und anderen Spielende-Bedingungen.

## KERNKOMPONENTEN

Das Projekt `ChessLogic` gliedert sich in folgende Hauptbereiche:

### 1. `Board.cs`
Das Herzstück der Brettdarstellung.
* Verwaltet ein 8x8-Array zur Positionierung der Schachfiguren.
* Speichert Informationen zu speziellen Zugmöglichkeiten, insbesondere für En Passant (`pawnSkipPositions`).
* Bietet Methoden zur Überprüfung des Brettzustands (z.B. `IsEmpty`, `IsInside`).
* Initialisiert die Standard-Startaufstellung der Figuren (`AddStartPieces`).
* Unterstützt das Erstellen einer tiefen Kopie des Bretts (`Copy`).
* Ermittelt Rochaderechte (`CastleRightKS`, `CastleRightQS`) und prüft auf die Möglichkeit von En-Passant-Schlägen (`CanCaptureEnPassant`).
* Beinhaltet Logik zur Erkennung von Remis durch unzureichendes Material (`InsufficientMaterial`).

### 2. `GameState.cs`
Verwaltet den dynamischen Zustand einer Schachpartie.
* Verfolgt, welcher Spieler aktuell am Zug ist (`CurrentPlayer`).
* Hält die aktuelle Brettkonfiguration (`Board`).
* Speichert das Spielergebnis (`Result`), sobald die Partie beendet ist.
* Implementiert die 50-Züge-Regel (`noCaptureOrPawnMoves`).
* Nutzt `StateString` zur Erkennung der dreifachen Stellungswiederholung (`stateHistory`).
* Stellt Methoden zur Verfügung, um alle legalen Züge für eine Figur (`LegalMovesForPiece`) oder für den gesamten Spieler (`AllLegalMovesFor`) zu ermitteln.
* Prüft nach jedem Zug auf Spielende-Bedingungen (`CheckForGameOver`).
* Aktualisiert den Zustand nach einem Zug (`UpdateStateAfterMove`).

### 3. `Pieces/` Verzeichnis
Enthält die Definitionen und die spezifische Logik für jede Schachfigur.

* **`Piece.cs` (Abstrakte Basisklasse)**
    * Definiert gemeinsame Eigenschaften aller Figuren: `Type` (Figurentyp), `Color` (Spielerfarbe) und `HasMoved` (wichtig für Rochade und Bauern-Doppelschritt).
    * Deklariert abstrakte Methoden `Copy()` (für tiefe Kopien) und `GetMoves()` (zur Ermittlung der Zugmöglichkeiten einer Figur ohne Berücksichtigung von Selbstschach).
    * Bietet Hilfsmethoden (`MovePositionsInDir`, `MovePositionsInDirs`) zur Berechnung von Zügen entlang von Linien und Diagonalen.
    * Enthält eine virtuelle Methode `CanCaptureOpponentKing`, um zu prüfen, ob eine Figur den gegnerischen König bedroht.

* **Spezifische Figurenklassen** (`Bishop.cs`, `King.cs`, `Knight.cs`, `Pawn.cs`, `Queen.cs`, `Rook.cs`):
    * Erben von `Piece` und implementieren deren abstrakte Methoden.
    * Definieren die spezifischen Bewegungsregeln und -richtungen für den jeweiligen Figurentyp.
    * Beispiele:
        * `Pawn.cs`: Logik für Vorwärtsbewegung, Doppelschritt, diagonales Schlagen, En Passant und Bauernumwandlung.
        * `King.cs`: Logik für Standard-Königszüge und Rochade (`CanCastleKingSide`, `CanCastleQueenSide`).
        * `Rook.cs`, `Bishop.cs`, `Queen.cs`: Nutzen primär `MovePositionsInDirs` mit ihren jeweiligen Richtungsarrays.
        * `Knight.cs`: Implementiert die L-förmige Springerbewegung.

### 4. `Moves/` Verzeichnis
Definiert die verschiedenen Arten von Schachzügen und deren Ausführung.

* **`Move.cs` (Abstrakte Basisklasse)**
    * Grundlegende Eigenschaften eines Zugs: `Type` (Art des Zugs), `FromPos` (Startposition), `ToPos` (Zielposition).
    * Abstrakte Methode `Execute(Board board)`, die den Zug auf dem Brett ausführt und zurückgibt, ob es ein Bauern- oder Schlagzug war (relevant für die 50-Züge-Regel).
    * Virtuelle Methode `IsLegal(Board board)`, die prüft, ob der Zug den eigenen König in ein Schachgebot stellen würde.

* **Spezifische Zugklassen** (`NormalMove.cs`, `Castle.cs`, `DoublePawn.cs`, `EnPassant.cs`, `PawnPromotion.cs`, `TeleportMove.cs`, `PositionSwapMove.cs`):
    * Erben von `Move` und implementieren die spezifische Logik für die Ausführung (`Execute`) und teilweise für die Legalitätsprüfung (`IsLegal`) des jeweiligen Zugtyps.
    * `NormalMove.cs`: Standardbewegung oder Schlag einer Figur.
    * `Castle.cs`: Führt die Rochade aus (Königs- und Turmzug).
    * `DoublePawn.cs`: Bauern-Doppelschritt, setzt En-Passant-Ziel.
    * `EnPassant.cs`: Führt einen En-Passant-Schlag aus.
    * `PawnPromotion.cs`: Wandelt einen Bauern auf der letzten Reihe in eine andere Figur um.
    * `TeleportMove.cs`, `PositionSwapMove.cs`: Repräsentieren spezielle Züge, die typischerweise durch Karten ausgelöst werden (Figur teleportieren, Positionen zweier eigener Figuren tauschen).

## HILFSKLASSEN UND ENUMERATIONEN

Diese Komponenten unterstützen die Kernlogik:

* **`Utilities/` Verzeichnis**
    * `Direction.cs`: Definiert Bewegungsrichtungen auf dem Brett (z.B. Nord, Südwest) als Zeilen- und Spaltenänderungen und ermöglicht deren Kombination.
    * `Position.cs`: Stellt eine Koordinate (Zeile, Spalte) auf dem Schachbrett dar und unterstützt Positionsarithmetik durch Addition von `Direction`-Objekten.

* **Weitere Hilfsklassen und Enumerationen im Hauptverzeichnis:**
    * `Counting.cs`: Zählt Figuren auf dem Brett nach Typ und Farbe, nützlich für die Remisprüfung bei unzureichendem Material.
    * `EndReason.cs`: Enumeration der verschiedenen Gründe für ein Spielende (z.B. `Checkmate`, `Stalemate`, `FiftyMoveRule`).
    * `MoveType.cs`: Enumeration der verschiedenen Arten von Schachzügen (z.B. `Normal`, `CastleKS`, `PawnPromotion`).
    * `PieceType.cs`: Enumeration der Figurentypen (z.B. `Pawn`, `Rook`, `King`).
    * `Player.cs`: Enumeration für die Spieler (Weiss, Schwarz, Keiner) und eine Erweiterungsmethode `Opponent()`.
    * `Result.cs`: Kapselt das Spielergebnis, inklusive des Gewinners und des Grundes für das Spielende.
    * `StateString.cs`: Erzeugt eine textuelle Repräsentation des Spielzustands (ähnlich FEN), die primär für die Erkennung der dreifachen Stellungswiederholung dient.

## DESIGNPRINZIPIEN

`ChessLogic` ist als eigenständiges Modul konzipiert. Es enthält keine Abhängigkeiten zu Benutzeroberflächen-Frameworks oder Netzwerktechnologien. Dies gewährleistet eine hohe Wiederverwendbarkeit und Testbarkeit der Kernspiellogik, sodass sie als Fundament für verschiedenartige Schachanwendungen dienen kann.

## PROJEKTDATEI

* **`ChessLogic.csproj`**: Die .NET-Projektdatei, die das Ziel-Framework (net9.0) und andere Projekteinstellungen für das `ChessLogic`-Modul definiert.