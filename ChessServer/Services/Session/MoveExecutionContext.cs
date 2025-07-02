using ChessLogic;
using ChessNetwork.DTOs;
using ChessServer.Services.Cards;

namespace ChessServer.Services.Session;

// Kapselt alle für die Ausführung eines Zugs notwendigen Daten und Dienste.
public record MoveExecutionContext(
    GameSession Session,                // Die aktuelle Spielsitzung, die den Kontext für den Zug bereitstellt.
    GameState State,                    // Der aktuelle Zustand des Spiels, einschliesslich des Schachbretts und der Spieler.
    IPlayerManager PlayerManager,       // Verwaltet die Spieler innerhalb der Sitzung.
    ICardManager CardManager,           // Verwaltet die Kartendienste und -logik.
    IHistoryManager HistoryManager,     // Verwaltet den Spielverlauf.
    GameTimerService TimerService,      // Verwaltet die Spielzeit und Timer-Ereignisse.
    MoveDto MoveDto,                    // Die DTO-Repräsentation des ausgeführten Zugs.
    Guid PlayerId                       // Die ID des Spielers, der den Zug ausführt.
);