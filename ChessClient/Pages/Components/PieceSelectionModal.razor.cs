// File: [SolutionDir]/ChessClient/Pages/Components/PieceSelectionModal.razor.cs
using ChessLogic;
using ChessNetwork.DTOs;
using ChessClient.Extensions;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessClient.Models; // Hinzugefügt für PieceSelectionChoiceInfo

namespace ChessClient.Pages.Components
{
    public partial class PieceSelectionModal : ComponentBase
    {
        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public string Title { get; set; } = "Figur wählen";
        [Parameter] public string PromptMessage { get; set; } = "Wähle eine Figur:";
        // GEÄNDERT: Typ von List<PieceType>? zu List<PieceSelectionChoiceInfo>?
        [Parameter] public List<PieceSelectionChoiceInfo>? Choices { get; set; }
        [Parameter] public Player PlayerColor { get; set; }
        [Parameter] public EventCallback<PieceType> OnPieceSelected { get; set; }
        [Parameter] public EventCallback OnCancelled { get; set; }
        [Parameter] public bool ShowCancelButton { get; set; } = true;

        private PieceType? SelectedPieceType { get; set; }
        private string ModalErrorMessage { get; set; } = "";
        private bool _selectedChoiceIsRevivable;

        protected override void OnParametersSet()
        {
            if (!IsVisible)
            {
                SelectedPieceType = null;
                ModalErrorMessage = "";
                _selectedChoiceIsRevivable = false;
            }
        }

        // GEÄNDERT: Akzeptiert PieceSelectionChoiceInfo
        private void SelectPieceType(PieceSelectionChoiceInfo choiceInfo)
        {
            if (!choiceInfo.CanBeRevivedOnBoard)
            {
                ModalErrorMessage = choiceInfo.TooltipMessage ?? $"{choiceInfo.Type} kann nicht wiederbelebt werden (Startfelder besetzt).";
                // Optional: Auswahl zurücksetzen oder als ungültig markieren, wenn versucht wird, eine deaktivierte Option zu wählen
                // SelectedPieceType = null; 
                // _selectedChoiceIsRevivable = false;
                // Aktuell wird der Klick nicht verhindert, aber der Bestätigen-Button würde es abfangen.
                // Besser wäre es, den Klick im UI zu verhindern oder hier die Auswahl nicht zu setzen.
                // Fürs Erste belassen wir es so, dass der Fehler angezeigt wird.
                // Wenn der Benutzer dann bestätigt, greift die Logik im HandleConfirm.
                // Um es klarer zu machen, setzen wir die Auswahl nur, wenn es geht:
                if (SelectedPieceType == choiceInfo.Type) // Wenn die bereits ausgewählte (ungültige) Option erneut geklickt wird
                {
                    SelectedPieceType = null; // Auswahl aufheben
                    _selectedChoiceIsRevivable = false;
                }
            }
            else
            {
                SelectedPieceType = choiceInfo.Type;
                _selectedChoiceIsRevivable = true;
                ModalErrorMessage = "";
            }
            StateHasChanged();
        }

        private async Task HandleConfirm()
        {
            if (SelectedPieceType.HasValue && _selectedChoiceIsRevivable)
            {
                await OnPieceSelected.InvokeAsync(SelectedPieceType.Value);
            }
            else if (SelectedPieceType.HasValue && !_selectedChoiceIsRevivable)
            {
                // Diese Meldung wird bereits in SelectPieceType gesetzt, falls eine invalide Option geklickt wird.
                // Hier stellen wir sicher, dass der Fehler auch da ist, falls der State anders zustande kam.
                var choice = Choices?.FirstOrDefault(c => c.Type == SelectedPieceType.Value);
                ModalErrorMessage = choice?.TooltipMessage ?? $"{SelectedPieceType.Value} kann nicht wiederbelebt werden (Startfelder besetzt). Bitte wähle eine andere Figur oder breche ab.";
            }
            else
            {
                ModalErrorMessage = "Bitte wähle eine gültige Figur aus.";
            }
            // StateHasChanged(); // Wird durch OnPieceSelected oder UI-Interaktion ausgelöst
        }

        private async Task HandleCancel()
        {
            await OnCancelled.InvokeAsync();
        }

        // Diese Methode ist statisch und kann hier verbleiben oder in eine Helper-Klasse ausgelagert werden.
        // Da sie spezifisch für die Anzeige in diesem Modal ist, ist sie hier gut aufgehoben.
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
                // Bauern sind typischerweise nicht Teil der Auswahl hier, aber falls doch:
                // (Player.White, PieceType.Pawn) => PieceDto.WhitePawn, 
                // (Player.Black, PieceType.Pawn) => PieceDto.BlackPawn,
                _ => null // Sollte nicht für Standard-Umwandlungs- oder Wiedergeburtsoptionen auftreten
            };
        }
    }
}