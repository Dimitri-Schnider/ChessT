using ChessNetwork.DTOs;

namespace ChessServer.Services.Session;

// Definiert den Vertrag für den Dienst, der einen Spielzug verarbeitet.
public interface IMoveExecutionService
{
    MoveResultDto ExecuteMove(MoveExecutionContext context);
}