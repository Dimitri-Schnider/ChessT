# ChessNetwork

Dieses Projekt ist verantwortlich für die Definition der Datenstrukturen (Data Transfer Objects - DTOs) und der Kommunikationsschnittstellen, die für den Datenaustausch zwischen dem `ChessClient` und dem `ChessServer` verwendet werden. Es stellt sicher, dass beide Seiten eine gemeinsame Sprache für Spielzustände, Züge, Spielerinformationen und andere spielrelevante Daten sprechen.

## Architektur und Dateistruktur

Die Hauptkomponenten dieses Projekts sind:

* **`DTOs/` Verzeichnis**: Dieses Verzeichnis enthält alle Data Transfer Objects, die für die Kommunikation zwischen Client und Server serialisiert und deserialisiert werden. Dazu gehören:
    * **`ActivateCardRequestDto.cs`**: Überträgt Informationen zur Aktivierung einer Spielkarte, einschliesslich der Karten-ID und optionaler Start-/Zielkoordinaten für bestimmte Kartentypen.
    * **`BoardDto.cs`**: Repräsentiert den Zustand des Schachbretts als ein 2D-Array von `PieceDto?` (nullable PieceDto).
    * **`CreateGameDto.cs`**: Enthält die Parameter, die zum Erstellen eines neuen Spiels benötigt werden, wie Spielername, gewünschte Farbe und initiale Bedenkzeit.
    * **`CreateGameResultDto.cs`**: Gibt das Ergebnis einer Spielerstellung zurück, einschliesslich Spiel-ID, Spieler-ID, zugewiesener Farbe und initialem Brettzustand.
    * **`GameHistoryDto.cs`**: Ein umfassendes DTO für den detaillierten Spielverlauf, das Informationen über die Spieler, Startzeit, Endzeit, Gewinner, Endgrund, alle Züge und alle gespielten Karten enthält.
    * **`GameInfoDto.cs`**: Liefert grundlegende Informationen zu einem Spiel, wie die ID des Erstellers, dessen Farbe und ob bereits ein Gegner beigetreten ist.
    * **`JoinDto.cs`**: Wird verwendet, wenn ein Spieler einem Spiel beitreten möchte und enthält dessen Namen.
    * **`JoinGameResultDto.cs`**: Enthält das Ergebnis des Beitritts zu einem Spiel, wie Spieler-ID, Name, zugewiesene Farbe und aktuellen Brettzustand.
    * **`MoveDto.cs`**: Dient zur Übermittlung eines Zugs vom Client zum Server, inklusive Start- und Zielkoordinaten, Spieler-ID und optional dem Figurentyp bei einer Bauernumwandlung.
    * **`MoveResultDto.cs`**: Gibt das Ergebnis eines Zugversuchs zurück, inklusive Gültigkeit, Fehlermeldung (falls vorhanden), neuem Brettzustand, ob der Spieler weiterhin am Zug ist (relevant für Karten wie Extrazug) und dem aktuellen Spielstatus. Es enthält auch `PlayerIdToSignalCardDraw`, um einen Kartenzug für einen Spieler auszulösen.
    * **`PieceDto.cs`**: Ein Enum, das alle Schachfiguren und deren Farben für die Netzwerkübertragung repräsentiert (z.B. `WhiteKing`, `BlackPawn`).
    * **`PlayedCardDto.cs`**: Speichert detaillierte Informationen über eine im Spiel gespielte Karte für den Spielverlauf.
    * **`PlayedMoveDto.cs`**: Enthält detaillierte Informationen zu einem einzelnen Zug im Spielverlauf, wie Zugnummer, Spieler, Koordinaten, Zugtyp, Zeitverbrauch und ggf. geschlagene Figur.
    * **`PlayerDto.cs`**: Ein Record zur Übertragung von Spielerinformationen (ID und Name).
    * **`TimeUpdateDto.cs`**: Wird verwendet, um die verbleibenden Bedenkzeiten beider Spieler und den aktuell am Zug befindlichen Spieler zu übermitteln.

* **`IGameSession.cs`**: Definiert den Vertrag (Interface) für die Interaktion mit einer Spielsitzung vom Client aus. Es listet Methoden auf, die der Server implementieren muss, um Spielaktionen wie das Erstellen und Beitreten zu Spielen, das Abrufen des Brettzustands, das Senden von Zügen und das Aktivieren von Karten zu ermöglichen.

* **`HttpGameSession.cs`**: Eine konkrete Implementierung von `IGameSession`, die `HttpClient` verwendet, um HTTP-Anfragen an die API-Endpunkte des `ChessServer` zu senden. Diese Klasse ist dafür zuständig, die DTOs zu serialisieren/deserialisieren und die Kommunikation mit dem Server zu handhaben.

* **`ChessNetwork.csproj`**: Die Projektdatei, die Abhängigkeiten wie `System.Net.Http.Json` für die JSON-Serialisierung/-Deserialisierung über HTTP und einen Projektverweis auf `ChessLogic` (da einige DTOs Typen aus `ChessLogic` verwenden, z.B. `Player`, `PieceType`) definiert.

Dieses Projekt stellt eine klare Trennung zwischen der Spiellogik (`ChessLogic`) und den Details der Netzwerkkommunikation her, was die Wartbarkeit und Testbarkeit verbessert.