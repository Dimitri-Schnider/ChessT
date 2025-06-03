# ChessLogic

Dieses Projekt bildet das Kernstück der Schachlogik für die gesamte Anwendung. Es ist verantwortlich für die Implementierung des Schachbretts, die Bewegungsregeln der einzelnen Figuren, die Validierung von Zügen und die Verwaltung des grundlegenden Spielzustands, einschliesslich der Erkennung von Schach, Matt und anderen Spielende-Bedingungen.

## Architektur und Dateistruktur

Das Projekt ist in mehrere Schlüsselkomponenten unterteilt, die zusammen die Funktionalität des Schachspiels abbilden:

* **`Board.cs`**: Repräsentiert das 8x8 Schachbrett. Es verwaltet die Positionen aller Figuren, speichert Informationen für spezielle Züge wie En Passant und bietet Methoden zur Überprüfung des Brettzustands (z.B. ob ein Feld leer ist oder innerhalb der Grenzen liegt). Enthält Logik für die Startaufstellung und das Kopieren des Bretts.
* **`GameState.cs`**: Verwaltet den übergeordneten Zustand einer Partie. Dies beinhaltet, welcher Spieler am Zug ist, das aktuelle Brett, das Spielergebnis (falls vorhanden), die Verfolgung der 50-Züge-Regel und die Erkennung von dreifacher Stellungswiederholung mittels `StateString`. Es prüft auf Spielende-Bedingungen und ermittelt legale Züge.
* **`Pieces/` Verzeichnis**: Enthält die Definitionen für alle Schachfiguren.
    * **`Piece.cs`**: Eine abstrakte Basisklasse für alle Figuren, die gemeinsame Eigenschaften wie `Type` (Figurentyp), `Color` (Farbe) und `HasMoved` (ob die Figur bereits bewegt wurde) definiert. Sie deklariert abstrakte Methoden wie `Copy()` und `GetMoves()`.
    * Spezifische Figurenklassen (`Pawn.cs`, `Rook.cs`, `Knight.cs`, `Bishop.cs`, `Queen.cs`, `King.cs`): Jede dieser Klassen erbt von `Piece` und implementiert die spezifischen Bewegungsregeln und Eigenschaften für den jeweiligen Figurentyp. Beispielsweise enthält `King.cs` Logik für die Rochade und `Pawn.cs` für den Doppelschritt, En Passant und die Bauernumwandlung.
* **`Moves/` Verzeichnis**: Definiert die verschiedenen Arten von Schachzügen.
    * **`Move.cs`**: Eine abstrakte Basisklasse, die Eigenschaften wie `Type`, `FromPos` (Startposition) und `ToPos` (Zielposition) sowie die Methoden `Execute()` (führt den Zug aus) und `IsLegal()` (prüft die Legalität) definiert.
    * Spezifische Zugklassen (`NormalMove.cs`, `Castle.cs`, `DoublePawn.cs`, `EnPassant.cs`, `PawnPromotion.cs`, `TeleportMove.cs`, `PositionSwapMove.cs`): Diese Klassen erben von `Move` und implementieren die Logik für die Ausführung und Validierung des jeweiligen Zugtyps. `TeleportMove` und `PositionSwapMove` repräsentieren spezielle Züge, die typischerweise durch Karten ausgelöst werden.
* **`Utilities/` Verzeichnis**: Enthält Hilfsklassen.
    * **`Direction.cs`**: Repräsentiert Richtungen auf dem Schachbrett (z.B. Nord, Südwest) als Vektoränderungen.
    * **`Position.cs`**: Stellt eine Koordinate auf dem Brett dar (Zeile und Spalte) und bietet Methoden zur Positionsarithmetik.
* **`Counting.cs`**: Eine Klasse zum Zählen der Anzahl verschiedener Figurentypen auf dem Brett, nützlich für die Überprüfung auf unzureichendes Material für ein Matt.
* **`EndReason.cs`**: Ein Enum, das die verschiedenen Gründe für das Ende einer Partie auflistet (z.B. `Checkmate`, `Stalemate`).
* **`MoveType.cs`**: Ein Enum, das die verschiedenen Arten von Zügen definiert (z.B. `Normal`, `CastleKS`, `PawnPromotion`).
* **`PieceType.cs`**: Ein Enum, das die Typen der Schachfiguren auflistet (z.B. `Pawn`, `Rook`, `King`).
* **`Player.cs`**: Ein Enum zur Darstellung der Spieler (Weiss, Schwarz, Keiner) und eine Erweiterungsmethode `Opponent()`.
* **`Result.cs`**: Eine Klasse zur Darstellung des Spielergebnisses, inklusive Gewinner und Grund des Spielendes.
* **`StateString.cs`**: Generiert eine textuelle Repräsentation des aktuellen Spielzustands, ähnlich der Forsyth-Edwards-Notation (FEN), die für die Erkennung von dreifacher Stellungswiederholung verwendet wird.
* **`ChessLogic.csproj`**: Die Projektdatei für das ChessLogic-Modul, die das Ziel-Framework und andere Projekteinstellungen definiert.

Dieses Projekt ist so konzipiert, dass es unabhängig von der Benutzeroberfläche oder der Netzwerkkommunikation funktioniert und eine solide Basis für jede Schachanwendung bietet.