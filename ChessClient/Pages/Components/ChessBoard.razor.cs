using ChessNetwork.DTOs;
using ChessNetwork;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ChessLogic;
using ChessClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using ChessClient.Models;
using ChessClient.Extensions;
using ChessClient.State;

namespace ChessClient.Pages.Components
{
    // Die Code-Behind-Klasse für die ChessBoard-Komponente.
    // Sie verwaltet die Logik für Spieler-Interaktionen wie Klicks und Drag-and-Drop
    // und kommuniziert mit der übergeordneten Komponente (Chess.razor) über EventCallbacks.
    public partial class ChessBoard : ComponentBase
    {
        // --- PARAMETER ---
        // Die folgenden Properties werden von der übergeordneten Komponente (Chess.razor) befüllt.

        [Parameter] public Guid GameId { get; set; }                                            // Die eindeutige ID des aktuellen Spiels.
        [Parameter] public BoardDto Board { get; set; } = null!;                                // Das Datenobjekt, das die aktuelle Brettstellung enthält.
        [Parameter] public EventCallback<MoveDto> OnMove { get; set; }                          // Callback, der ausgelöst wird, wenn ein gültiger Zug gemacht wird.
        [Parameter] public bool FlipBoard { get; set; }                                         // Bestimmt, ob das Brett gedreht dargestellt wird (für den schwarzen Spieler).
        [Parameter] public Guid PlayerId { get; set; }                                          // Die ID des aktuellen Spielers.
        [Parameter] public bool IsEnabled { get; set; } = true;                                 // Gibt an, ob das Brett für Interaktionen freigegeben ist.
        [Parameter] public Player MyPlayerColor { get; set; }                                   // Die Farbe des aktuellen Spielers.

        // Parameter für Karteneffekte
        [Parameter] public EventCallback<string> OnSquareClickForCard { get; set; }             // Callback für Klicks, wenn eine Kartenaktion aktiv ist.
        [Parameter] public bool IsSquareSelectionModeActiveForCard { get; set; }                // Flag, das anzeigt, ob gerade auf eine Feldauswahl für eine Karte gewartet wird.
        [Parameter] public Player? PlayerColorForCardPieceSelection { get; set; }               // Definiert, welche Farbe für die Figurenauswahl bei einem Karteneffekt relevant ist.
        [Parameter] public string? FirstSelectedSquareForCard { get; set; }                     // Speichert die Koordinate des ersten Klicks bei einer mehrstufigen Kartenaktion.
        [Parameter] public List<string>? HighlightedCardTargetSquaresForSelection { get; set; } // Liste der Felder, die als gültige Ziele für eine Kartenaktion markiert sind.


        // --- INJECTIONS ---
        // Injizierte Dienste für Serverkommunikation und JavaScript-Interop.

        [Inject] private IGameSession Game { get; set; } = null!;       // Dienst für die Kommunikation mit der Game-API.
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;    // Für die Interaktion mit JavaScript (z.B. für Drag-and-Drop).

        // --- PRIVATE FELDER ---
        // Interner Zustand der Komponente.

        private string? selectedFrom;                                   // Speichert die Koordinate des Feldes, von dem aus ein Zug gestartet wird (z.B. "e2").
        private List<string> legalMovesForHighlight = new();            // Liste der legalen Züge für die aktuell ausgewählte Figur.
        private bool _isCurrentlyDragging;                              // Flag, das anzeigt, ob gerade eine Figur per Drag-and-Drop bewegt wird.

        // Wird aufgerufen, wenn der Benutzer beginnt, eine Figur zu ziehen.
        private async Task HandlePieceDragStart(string coordOfPieceStartingDrag)
        {
            if (!IsEnabled || IsSquareSelectionModeActiveForCard) return; // Dragging nur erlauben, wenn das Brett aktiv und kein Kartenmodus an ist.

            (int r, int f) = PositionHelper.ToIndices(coordOfPieceStartingDrag);
            PieceDto? pieceOnSquare = (Board?.Squares != null && r >= 0 && r < 8 && f >= 0 && f < 8) ? Board.Squares[r][f] : null;

            // Prüft, ob auf dem Feld eine eigene Figur steht.
            if (pieceOnSquare.HasValue && pieceOnSquare.Value.IsOfPlayerColor(MyPlayerColor))
            {
                _isCurrentlyDragging = true;
                selectedFrom = coordOfPieceStartingDrag;
                try
                {
                    // Lädt die legalen Züge vom Server und speichert sie für die Hervorhebung.
                    legalMovesForHighlight = (await Game.GetLegalMovesAsync(GameId, selectedFrom, PlayerId)).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChessBoard] Error getting legal moves for drag: {ex.Message}");
                    legalMovesForHighlight.Clear();
                }
            }
            else
            {
                // Wenn keine eigene Figur, wird der Zustand zurückgesetzt.
                selectedFrom = null;
                legalMovesForHighlight.Clear();
            }
            await InvokeAsync(StateHasChanged); // UI aktualisieren.
        }

        // Wird aufgerufen, wenn eine gezogene Figur auf einem Feld losgelassen wird.
        private async Task HandleSquareDrop(string targetCoord, string pieceCoordThatWasDragged)
        {
            if (IsSquareSelectionModeActiveForCard) return; // Keine Züge erlauben, wenn Kartenmodus aktiv ist.

            // Prüft, ob die Figur auf ein legales Zielfeld gezogen wurde.
            if (selectedFrom != null && selectedFrom == pieceCoordThatWasDragged && IsEnabled && legalMovesForHighlight.Contains(targetCoord))
            {
                // Löst das OnMove-Event aus, um die übergeordnete Komponente über den Zug zu informieren.
                await OnMove.InvokeAsync(new MoveDto(selectedFrom, targetCoord, PlayerId));
            }
            // Setzt den Drag-Zustand nach dem Drop zurück.
            selectedFrom = null;
            legalMovesForHighlight.Clear();
            _isCurrentlyDragging = false;
            await InvokeAsync(StateHasChanged);
        }

        // Wird aufgerufen, wenn der Drag-Vorgang endet (egal ob auf einem gültigen Ziel oder nicht).
        private async Task HandlePieceDragEnd((string pieceCoord, bool droppedOnTarget) args)
        {
            if (IsSquareSelectionModeActiveForCard)
            {
                _isCurrentlyDragging = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            // Wenn die Figur nicht auf einem Zielfeld fallengelassen wurde, wird die Auswahl aufgehoben.
            if (!args.droppedOnTarget)
            {
                selectedFrom = null;
                legalMovesForHighlight.Clear();
            }
            _isCurrentlyDragging = false; // Beendet den Drag-Modus in jedem Fall.
            await InvokeAsync(StateHasChanged);
        }

        // Behandelt Klicks auf ein Feld. Dies ist die zentrale Logik für Züge per Klick.
        private async Task OnSquareClick(string coord)
        {
            if (_isCurrentlyDragging || !IsEnabled || Board?.Squares == null) return; // Ignoriert Klicks während eines Drags oder wenn das Brett deaktiviert ist.

            // --- Logik für Karteneffekte ---
            if (IsSquareSelectionModeActiveForCard)
            {
                // Wenn ein Karteneffekt aktiv ist, werden Klicks an einen speziellen Handler weitergeleitet.
                if (HighlightedCardTargetSquaresForSelection != null && HighlightedCardTargetSquaresForSelection.Count > 0)
                {
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
                await OnSquareClickForCard.InvokeAsync(coord);
                return;
            }

            // --- Logik für normale Züge ---
            (int rank, int file) = PositionHelper.ToIndices(coord);
            PieceDto? pieceOnSquare = (rank >= 0 && rank < 8 && file >= 0 && file < 8) ? Board.Squares[rank][file] : null;

            if (selectedFrom is null) // Fall 1: Noch keine Figur ausgewählt.
            {
                // Wenn eine eigene Figur angeklickt wird, wird sie ausgewählt.
                if (pieceOnSquare.HasValue && pieceOnSquare.Value.IsOfPlayerColor(MyPlayerColor))
                {
                    selectedFrom = coord;
                    legalMovesForHighlight.Clear();
                    try
                    {
                        // Lädt die legalen Züge für die Hervorhebung.
                        legalMovesForHighlight = (await Game.GetLegalMovesAsync(GameId, coord, PlayerId)).ToList();
                    }
                    catch (Exception) { legalMovesForHighlight.Clear(); }
                }
            }
            else // Fall 2: Bereits eine Figur ausgewählt.
            {
                if (coord == selectedFrom) // Klick auf die bereits ausgewählte Figur hebt die Auswahl auf.
                {
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
                else if (legalMovesForHighlight.Contains(coord)) // Klick auf ein legales Zielfeld führt den Zug aus.
                {
                    await OnMove.InvokeAsync(new MoveDto(selectedFrom, coord, PlayerId));
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
                else if (pieceOnSquare.HasValue && pieceOnSquare.Value.IsOfPlayerColor(MyPlayerColor)) // Klick auf eine andere eigene Figur wechselt die Auswahl.
                {
                    selectedFrom = coord;
                    legalMovesForHighlight.Clear();
                    try
                    {
                        legalMovesForHighlight = (await Game.GetLegalMovesAsync(GameId, coord, PlayerId)).ToList();
                    }

                    catch (Exception) { legalMovesForHighlight.Clear(); }
                }
                else // Klick auf ein ungültiges Feld hebt die Auswahl auf.
                {
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
            }
            await InvokeAsync(StateHasChanged); // UI aktualisieren.
        }

        // Lifecycle-Methode, die bei jeder Parameter-Änderung aufgerufen wird.
        // Stellt sicher, dass der interne Zustand konsistent bleibt (z.B. nach einem Zug des Gegners).
        protected override async Task OnParametersSetAsync()
        {
            if (Board?.Squares == null)
            {
                selectedFrom = null;
                legalMovesForHighlight.Clear();
                return;
            }

            // Wenn der Kartenmodus aktiv wird, werden normale Zug-Highlights und Auswahlen gelöscht.
            if (IsSquareSelectionModeActiveForCard &&
                ((HighlightedCardTargetSquaresForSelection?.Count ?? 0) > 0 || PlayerColorForCardPieceSelection.HasValue))
            {
                legalMovesForHighlight.Clear();
                selectedFrom = null;
            }
            // Wenn eine Figur ausgewählt war, wird geprüft, ob sie noch da ist und dem Spieler gehört.
            else if (selectedFrom != null && !IsSquareSelectionModeActiveForCard)
            {
                bool pieceStillExistsAndIsMine = false;
                (int r, int f) = PositionHelper.ToIndices(selectedFrom);
                if (r >= 0 && r < 8 && f >= 0 && f < 8 && Board.Squares[r][f].HasValue)
                {
                    PieceDto pieceValue = Board.Squares[r][f]!.Value;
                    pieceStillExistsAndIsMine = pieceValue.IsOfPlayerColor(MyPlayerColor);
                }

                if (!pieceStillExistsAndIsMine) // Wenn nicht, Auswahl zurücksetzen.
                {
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
                else // Wenn ja, legale Züge neu laden, falls sich die Situation geändert hat.
                {
                    try
                    {
                        legalMovesForHighlight = (await Game.GetLegalMovesAsync(GameId, selectedFrom, PlayerId)).ToList();
                    }
                    catch { legalMovesForHighlight.Clear(); }
                }
            }

            if (legalMovesForHighlight == null) legalMovesForHighlight = new List<string>();
            await base.OnParametersSetAsync();
        }
    }
}