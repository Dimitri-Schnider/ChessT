# ChessNetwork

Dieses Projekt ist verantwortlich für die Definition der Datenstrukturen (Data Transfer Objects - DTOs) und der Kommunikationsschnittstellen, die für den Datenaustausch zwischen dem `ChessClient` und dem `ChessServer` verwendet werden. Es stellt sicher, dass beide Seiten eine gemeinsame Sprache für Spielzustände, Züge, Spielerinformationen und andere spielrelevante Daten sprechen.

## Architektur und Dateistruktur

`ChessNetwork` ist das zentrale "Wörterbuch" der Anwendung und gliedert sich in folgende Hauptbereiche:

### `DTOs/` Verzeichnis
Dieses Verzeichnis enthält alle Data Transfer Objects (DTOs), die für die Kommunikation zwischen Client und Server serialisiert und deserialisiert werden.

* **`ActivateCardRequestDto.cs`**: Überträgt alle notwendigen Daten für die Aktivierung einer Spielkarte, inklusive optionaler Parameter für spezifische Effekte wie Zielkoordinaten oder Figurentypen.
* **`AffectedSquareInfo.cs`**: Beschreibt ein einzelnes Feld, das auf dem Client visuell hervorgehoben werden soll, typischerweise als Ergebnis eines Karteneffekts.
* **`BoardDto.cs`**: Repräsentiert den kompletten Brettzustand als 2D-Array von `PieceDto`-Enums.
* **`CapturedPieceTypeDto.cs`**: Überträgt den Typ einer geschlagenen Figur, relevant für den 'Wiedergeburt'-Effekt.
* **`CardDto.cs`**: Definiert eine Spielkarte mit ihrer eindeutigen Instanz-ID, Typ-ID, Namen, Beschreibung und Bild-URL.
* **`CardSwapAnimationDetailsDto.cs`**: Enthält die notwendigen Informationen für den Client, um die Tauschanimation zweier Karten korrekt darzustellen.
* **`CreateGameDto.cs`**: Kapselt alle Parameter für die Erstellung eines neuen Spiels, inklusive Spielername, Farbwahl, Zeit, Gegnertyp (Mensch/Computer) und Schwierigkeitsgrad.
* **`CreateGameResultDto.cs`**: Gibt das Ergebnis einer Spielerstellung zurück, inklusive Spiel-ID, Spieler-ID, zugewiesener Farbe und initialem Brettzustand.
* **`GameHistoryDto.cs`**: Ein umfassendes DTO für den detaillierten Spielverlauf, das Informationen über die Spieler, Startzeit, Endzeit, Gewinner, Endgrund, alle Züge und alle gespielten Karten enthält.
* **`GameInfoDto.cs`**: Liefert grundlegende Informationen zu einem Spiel, wie die ID des Erstellers, dessen Farbe und ob bereits ein Gegner beigetreten ist.
* **`InitialHandDto.cs`**: Übermittelt die Starthand eines Spielers sowie die Anzahl der Karten im Nachziehstapel.
* **`JoinDto.cs`**: Wird verwendet, wenn ein Spieler einem Spiel beitreten möchte, und enthält dessen Namen.
* **`JoinGameResultDto.cs`**: Enthält das Ergebnis des Beitritts zu einem Spiel, wie Spieler-ID, Name, zugewiesene Farbe und aktuellen Brettzustand.
* **`MoveDto.cs`**: Dient zur Übermittlung eines Zugs vom Client zum Server, inklusive Start- und Zielkoordinaten, Spieler-ID und optional dem Figurentyp bei einer Bauernumwandlung.
* **`MoveResultDto.cs`**: Gibt das Ergebnis eines Zugversuchs zurück, inklusive Gültigkeit, Fehlermeldung, neuem Brettzustand und aktuellem Spielstatus.
* **`OpponentInfoDto.cs`**: Dient zur Übertragung der Basisinformationen eines Gegners (ID, Name, Farbe).
* **`PieceDto.cs`**: Ein Enum, das alle Schachfiguren und deren Farben für die Netzwerkübertragung repräsentiert (z.B. `WhiteKing`, `BlackPawn`).
* **`PlayedCardDto.cs`**: Speichert detaillierte Informationen über eine im Spielverlauf gespielte Karte.
* **`PlayedMoveDto.cs`**: Enthält detaillierte Informationen zu einem einzelnen Zug im Spielverlauf, wie Zugnummer, Spieler, Koordinaten und Zeitverbrauch.
* **`PlayerDto.cs`**: Ein Record zur Übertragung von Spielerinformationen (ID und Name).
* **`PositionDto.cs`**: Repräsentiert eine Koordinate auf dem Brett (Zeile und Spalte) für die Netzwerkübertragung.
* **`ServerCardActivationResultDto.cs`**: Meldet das detaillierte Ergebnis einer Kartenaktivierung vom Server an den Client, inklusive möglicher Folgeaktionen wie das Ziehen einer neuen Karte.
* **`TimeUpdateDto.cs`**: Wird verwendet, um die verbleibenden Bedenkzeiten beider Spieler und den aktuell am Zug befindlichen Spieler zu übermitteln.

### `Configuration/` Verzeichnis
* **`CardConstants.cs`**: Definiert statische Konstanten für alle Karten-IDs (z.B. `ExtraZug`, `Teleport`), um "magische Strings" im Code zu vermeiden und die Wartbarkeit zu erhöhen.

### Schnittstellen und Implementierungen
* **`IGameSession.cs`**: Definiert den Vertrag (das Interface) für die Interaktion mit einer Spielsitzung. Es listet alle Methoden auf, die der Server für Spielaktionen implementieren muss, und stellt eine klare Abstraktion für den Client bereit.
* **`HttpGameSession.cs`**: Eine konkrete Implementierung von `IGameSession`, die `HttpClient` verwendet, um HTTP-Anfragen an die API-Endpunkte des `ChessServer` zu senden. Diese Klasse ist dafür zuständig, die DTOs zu serialisieren/deserialisieren und die gesamte HTTP-Kommunikation zu handhaben.

### `ChessNetwork.csproj`
Die Projektdatei definiert die Abhängigkeiten, wie `System.Net.Http.Json` für die JSON-Kommunikation und den wichtigen Projektverweis auf `ChessLogic`, da viele DTOs Typen aus der Kernlogik (z.B. `Player`, `PieceType`) verwenden.

---
Dieses Projekt stellt eine klare Trennung zwischen der Spiellogik (`ChessLogic`) und den Details der Netzwerkkommunikation her, was die Wartbarkeit und Testbarkeit des Gesamtsystems verbessert.