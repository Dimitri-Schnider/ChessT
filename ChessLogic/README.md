# ChessLogic

Dieses Projekt implementiert die Kernlogik des Schachspiels. Es ist verantwortlich für das Schachbrett, die Bewegungsregeln der Figuren, die Zugvalidierung und die Verwaltung des Spielzustands, einschliesslich der Erkennung von Schach, Matt und anderen Spielende-Bedingungen. [cite: 3570]

## KERNKOMPONENTEN

Das Projekt `ChessLogic` gliedert sich in folgende Hauptbereiche:

### 1. `Board.cs`
Das Herzstück der Brettdarstellung.
* Verwaltet ein 8x8-Array zur Positionierung der Schachfiguren. [cite: 3185, 3572]
* Speichert Informationen zu speziellen Zugmöglichkeiten, insbesondere für En Passant (`pawnSkipPositions`). [cite: 3187, 3572]
* Bietet Methoden zur Überprüfung des Brettzustands (z.B. `IsEmpty`, `IsInside`). [cite: 3572]
* Initialisiert die Standard-Startaufstellung der Figuren (`AddStartPieces`). [cite: 3198, 3572]
* Unterstützt das Erstellen einer tiefen Kopie des Bretts (`Copy`). [cite: 3220, 3572]
* Ermittelt Rochaderechte (`CastleRightKS`, `CastleRightQS`) und prüft auf die Möglichkeit von En-Passant-Schlägen (`CanCaptureEnPassant`). [cite: 3246, 3249, 3257]
* Beinhaltet Logik zur Erkennung von Remis durch unzureichendes Material (`InsufficientMaterial`). [cite: 3230]

### 2. `GameState.cs`
Verwaltet den dynamischen Zustand einer Schachpartie.
* Verfolgt, welcher Spieler aktuell am Zug ist (`CurrentPlayer`). [cite: 3283, 3574]
* Hält die aktuelle Brettkonfiguration (`Board`). [cite: 3283, 3574]
* Speichert das Spielergebnis (`Result`), sobald die Partie beendet ist. [cite: 3285, 3574]
* Implementiert die 50-Züge-Regel (`noCaptureOrPawnMoves`). [cite: 3286, 3574]
* Nutzt `StateString` zur Erkennung der dreifachen Stellungswiederholung (`stateHistory`). [cite: 3287, 3574]
* Stellt Methoden zur Verfügung, um alle legalen Züge für eine Figur (`LegalMovesForPiece`) oder für den gesamten Spieler (`AllLegalMovesFor`) zu ermitteln. [cite: 3289, 3306, 3574]
* Prüft nach jedem Zug auf Spielende-Bedingungen (`CheckForGameOver`). [cite: 3310, 3574]
* Aktualisiert den Zustand nach einem Zug (`UpdateStateAfterMove`). [cite: 3291]

### 3. `Pieces/` Verzeichnis
Enthält die Definitionen und die spezifische Logik für jede Schachfigur. [cite: 3576]

* **`Piece.cs` (Abstrakte Basisklasse)**
    * Definiert gemeinsame Eigenschaften aller Figuren: `Type` (Figurentyp), `Color` (Spielerfarbe) und `HasMoved` (wichtig für Rochade und Bauern-Doppelschritt). [cite: 3528, 3529, 3530, 3577]
    * Deklariert abstrakte Methoden `Copy()` (für tiefe Kopien) und `GetMoves()` (zur Ermittlung der Zugmöglichkeiten einer Figur ohne Berücksichtigung von Selbstschach). [cite: 3531, 3577]
    * Bietet Hilfsmethoden (`MovePositionsInDir`, `MovePositionsInDirs`) zur Berechnung von Zügen entlang von Linien und Diagonalen. [cite: 3533, 3540, 3577]
    * Enthält eine virtuelle Methode `CanCaptureOpponentKing`, um zu prüfen, ob eine Figur den gegnerischen König bedroht. [cite: 3542, 3577]

* **Spezifische Figurenklassen** (`Bishop.cs`, `King.cs`, `Knight.cs`, `Pawn.cs`, `Queen.cs`, `Rook.cs`):
    * Erben von `Piece` und implementieren deren abstrakte Methoden. [cite: 3578]
    * Definieren die spezifischen Bewegungsregeln und -richtungen für den jeweiligen Figurentyp. [cite: 3578]
    * Beispiele:
        * `Pawn.cs`: Logik für Vorwärtsbewegung, Doppelschritt, diagonales Schlagen, En Passant und Bauernumwandlung. [cite: 3490, 3579]
        * `King.cs`: Logik für Standard-Königszüge und Rochade (`CanCastleKingSide`, `CanCastleQueenSide`). [cite: 3447, 3458, 3461, 3579]
        * `Rook.cs`, `Bishop.cs`, `Queen.cs`: Nutzen primär `MovePositionsInDirs` mit ihren jeweiligen Richtungsarrays. [cite: 3578]
        * `Knight.cs`: Implementiert die L-förmige Springerbewegung. [cite: 3475, 3578]

### 4. `Moves/` Verzeichnis
Definiert die verschiedenen Arten von Schachzügen und deren Ausführung. [cite: 3580]

* **`Move.cs` (Abstrakte Basisklasse)**
    * Grundlegende Eigenschaften eines Zugs: `Type` (Art des Zugs), `FromPos` (Startposition), `ToPos` (Zielposition). [cite: 3363, 3364, 3365, 3580]
    * Abstrakte Methode `Execute(Board board)`, die den Zug auf dem Brett ausführt und zurückgibt, ob es ein Bauern- oder Schlagzug war (relevant für die 50-Züge-Regel). [cite: 3366, 3580]
    * Virtuelle Methode `IsLegal(Board board)`, die prüft, ob der Zug den eigenen König in ein Schachgebot stellen würde. [cite: 3367, 3580]

* **Spezifische Zugklassen** (`NormalMove.cs`, `Castle.cs`, `DoublePawn.cs`, `EnPassant.cs`, `PawnPromotion.cs`, `TeleportMove.cs`, `PositionSwapMove.cs`):
    * Erben von `Move` und implementieren die spezifische Logik für die Ausführung (`Execute`) und teilweise für die Legalitätsprüfung (`IsLegal`) des jeweiligen Zugtyps. [cite: 3581]
    * `NormalMove.cs`: Standardbewegung oder Schlag einer Figur. [cite: 3370, 3581]
    * `Castle.cs`: Führt die Rochade aus (Königs- und Turmzug). [cite: 3320, 3581]
    * `DoublePawn.cs`: Bauern-Doppelschritt, setzt En-Passant-Ziel. [cite: 3344, 3581]
    * `EnPassant.cs`: Führt einen En-Passant-Schlag aus. [cite: 3353, 3581]
    * `PawnPromotion.cs`: Wandelt einen Bauern auf der letzten Reihe in eine andere Figur um. [cite: 3379, 3581]
    * `TeleportMove.cs`, `PositionSwapMove.cs`: Repräsentieren spezielle Züge, die typischerweise durch Karten ausgelöst werden (Figur teleportieren, Positionen zweier eigener Figuren tauschen). [cite: 3392, 3411, 3581]

## HILFSKLASSEN UND ENUMERATIONEN

Diese Komponenten unterstützen die Kernlogik:

* **`Utilities/` Verzeichnis** [cite: 3582]
    * `Direction.cs`: Definiert Bewegungsrichtungen auf dem Brett (z.B. Nord, Südwest) als Zeilen- und Spaltenänderungen und ermöglicht deren Kombination. [cite: 3640, 3583]
    * `Position.cs`: Stellt eine Koordinate (Zeile, Spalte) auf dem Schachbrett dar und unterstützt Positionsarithmetik durch Addition von `Direction`-Objekten. [cite: 3655, 3584]

* **Weitere Hilfsklassen und Enumerationen im Hauptverzeichnis:**
    * `Counting.cs`: Zählt Figuren auf dem Brett nach Typ und Farbe, nützlich für die Remisprüfung bei unzureichendem Material. [cite: 3263, 3585]
    * `EndReason.cs`: Enumeration der verschiedenen Gründe für ein Spielende (z.B. `Checkmate`, `Stalemate`, `FiftyMoveRule`). [cite: 3277, 3586]
    * `MoveType.cs`: Enumeration der verschiedenen Arten von Schachzügen (z.B. `Normal`, `CastleKS`, `PawnPromotion`). [cite: 3431, 3587]
    * `PieceType.cs`: Enumeration der Figurentypen (z.B. `Pawn`, `Rook`, `King`). [cite: 3564, 3588]
    * `Player.cs`: Enumeration für die Spieler (Weiss, Schwarz, Keiner) und eine Erweiterungsmethode `Opponent()`. [cite: 3565, 3567, 3589]
    * `Result.cs`: Kapselt das Spielergebnis, inklusive des Gewinners und des Grundes für das Spielende. [cite: 3594, 3590]
    * `StateString.cs`: Erzeugt eine textuelle Repräsentation des Spielzustands (ähnlich FEN), die primär für die Erkennung der dreifachen Stellungswiederholung dient. [cite: 3603, 3591]

## DESIGNPRINZIPIEN

`ChessLogic` ist als eigenständiges Modul konzipiert. Es enthält keine Abhängigkeiten zu Benutzeroberflächen-Frameworks oder Netzwerktechnologien. Dies gewährleistet eine hohe Wiederverwendbarkeit und Testbarkeit der Kernspiellogik, sodass sie als Fundament für verschiedenartige Schachanwendungen dienen kann. [cite: 3593]

## PROJEKTDATEI

* **`ChessLogic.csproj`**: Die .NET-Projektdatei, die das Ziel-Framework (net9.0) und andere Projekteinstellungen für das `ChessLogic`-Modul definiert. [cite: 3592]