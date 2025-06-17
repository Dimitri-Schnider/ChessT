using ChessLogic;
using ChessNetwork.DTOs;
using ChessClient.Extensions;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessClient.Models;

namespace ChessClient.Pages.Components
{
    // Die Code-Behind-Klasse für das PieceSelectionModal. Verwaltet die Logik der Figurenauswahl.
    public partial class PieceSelectionModal : ComponentBase
    {
        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public string Title { get; set; } = "Figur wählen";
        [Parameter] public string PromptMessage { get; set; } = "Wähle eine Figur:";

        // Eine Liste von 'PieceSelectionChoiceInfo'-Objekten. Diese enthalten nicht nur den Figurentyp,
        // sondern auch die Information, ob die Auswahl gültig ist (CanBeRevivedOnBoard) und einen Tooltip.
        [Parameter] public List<PieceSelectionChoiceInfo>? Choices { get; set; }
        [Parameter] public Player PlayerColor { get; set; }                         // Die Farbe, für die die Figuren angezeigt werden sollen.
        [Parameter] public EventCallback<PieceType> OnPieceSelected { get; set; }   // Callback, der die ausgewählte Figur zurückgibt.
        [Parameter] public EventCallback OnCancelled { get; set; }                  // Callback, der bei einem Abbruch aufgerufen wird.
        [Parameter] public bool ShowCancelButton { get; set; } = true;              // Steuert die Sichtbarkeit des Abbrechen-Buttons.

        private PieceType? SelectedPieceType { get; set; }                          // Der vom Benutzer aktuell ausgewählte Figurentyp.
        private string ModalErrorMessage { get; set; } = "";                        // Eine eventuelle Fehlermeldung für den Benutzer.
        private bool _selectedChoiceIsRevivable;                                    // Interner Merker, ob die ausgewählte Option gültig ist.

        // Setzt den Zustand des Modals zurück, wenn es ausgeblendet wird.
        protected override void OnParametersSet()
        {
            if (!IsVisible)
            {
                SelectedPieceType = null;
                ModalErrorMessage = "";
                _selectedChoiceIsRevivable = false;
            }
        }

        // Behandelt den Klick auf eine der Figurenauswahl-Optionen.
        private void SelectPieceType(PieceSelectionChoiceInfo choiceInfo)
        {
            // Wenn die Auswahl deaktiviert ist (z.B. weil die Startfelder besetzt sind).
            if (!choiceInfo.CanBeRevivedOnBoard)
            {
                // Zeigt dem Benutzer eine erklärende Fehlermeldung an.
                ModalErrorMessage = choiceInfo.TooltipMessage ?? $"{choiceInfo.Type} kann nicht wiederbelebt werden.";
                if (SelectedPieceType == choiceInfo.Type) // Hebt die Auswahl auf, wenn erneut auf die ungültige Option geklickt wird.
                {
                    SelectedPieceType = null;
                    _selectedChoiceIsRevivable = false;
                }
            }
            else
            {
                // Wenn die Auswahl gültig ist, wird der Zustand aktualisiert und die Fehlermeldung gelöscht.
                SelectedPieceType = choiceInfo.Type;
                _selectedChoiceIsRevivable = true;
                ModalErrorMessage = "";
            }
            StateHasChanged();
        }

        // Behandelt den Klick auf den "Bestätigen"-Button.
        private async Task HandleConfirm()
        {
            if (SelectedPieceType.HasValue && _selectedChoiceIsRevivable)
            {
                // Wenn eine gültige Auswahl getroffen wurde, wird der OnPieceSelected-Callback ausgelöst.
                await OnPieceSelected.InvokeAsync(SelectedPieceType.Value);
            }
            else if (SelectedPieceType.HasValue && !_selectedChoiceIsRevivable)
            {
                // Stellt sicher, dass eine Fehlermeldung angezeigt wird, falls der Button trotz ungültiger Auswahl geklickt wird.
                var choice = Choices?.FirstOrDefault(c => c.Type == SelectedPieceType.Value);
                ModalErrorMessage = choice?.TooltipMessage ?? $"{SelectedPieceType.Value} kann nicht ausgewählt werden.";
            }
            else
            {
                ModalErrorMessage = "Bitte wähle eine gültige Figur aus.";
            }
        }

        // Behandelt den Klick auf den "Abbrechen"-Button.
        private async Task HandleCancel()
        {
            await OnCancelled.InvokeAsync();
        }

        // Statische Hilfsmethode, um aus Farbe und Figurentyp das passende PieceDto für die Bildanzeige zu ermitteln.
        protected internal static PieceDto? GetPieceDtoForDisplay(Player color, PieceType pieceType)
        {
            return (color, pieceType) switch
            {
                (Player.White, PieceType.Queen) => PieceDto.WhiteQueen,
                (Player.White, PieceType.Rook) => PieceDto.WhiteRook,
                (Player.White, PieceType.Bishop) => PieceDto.WhiteBishop,
                (Player.White, PieceType.Knight) => PieceDto.WhiteKnight,
                (Player.Black, PieceType.Queen) => PieceDto.BlackQueen,
                (Player.Black, PieceType.Rook) => PieceDto.BlackRook,
                (Player.Black, PieceType.Bishop) => PieceDto.BlackBishop,
                (Player.Black, PieceType.Knight) => PieceDto.BlackKnight,
                _ => null // Andere Fälle wie König oder Bauer sind hier nicht relevant.
            };
        }
    }
}