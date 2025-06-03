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
    public partial class ChessBoard : ComponentBase
    {
        [Parameter] public Guid GameId { get; set; }
        [Parameter] public BoardDto Board { get; set; } = null!;
        [Parameter] public EventCallback<MoveDto> OnMove { get; set; }
        [Parameter] public bool FlipBoard { get; set; }
        [Parameter] public Guid PlayerId { get; set; }
        [Parameter] public bool IsEnabled { get; set; } = true;
        [Parameter] public Player MyPlayerColor { get; set; }
        [Parameter] public EventCallback<string> OnSquareClickForCard { get; set; }
        [Parameter] public bool IsSquareSelectionModeActiveForCard { get; set; }
        [Parameter] public Player? PlayerColorForCardPieceSelection { get; set; }
        [Parameter] public string? FirstSelectedSquareForCard { get; set; }

        [Parameter] public List<string>? HighlightedCardTargetSquaresForSelection { get; set; }


        [Inject] private IGameSession Game { get; set; } = null!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

        private string? selectedFrom;
        private List<string> legalMovesForHighlight = new();
        private bool _isCurrentlyDragging;

        private async Task HandlePieceDragStart(string coordOfPieceStartingDrag)
        {
            if (!IsEnabled || IsSquareSelectionModeActiveForCard) return;
            (int r, int f) = PositionHelper.ToIndices(coordOfPieceStartingDrag);
            PieceDto? pieceOnSquare = (Board?.Squares != null && r >= 0 && r < 8 && f >= 0 && f < 8) ? Board.Squares[r][f] : null;

            if (pieceOnSquare.HasValue && pieceOnSquare.Value.IsOfPlayerColor(MyPlayerColor))
            {
                _isCurrentlyDragging = true;
                selectedFrom = coordOfPieceStartingDrag;
                try
                {
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
                selectedFrom = null;
                legalMovesForHighlight.Clear();
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleSquareDrop(string targetCoord, string pieceCoordThatWasDragged)
        {
            if (IsSquareSelectionModeActiveForCard) return;
            if (selectedFrom != null && selectedFrom == pieceCoordThatWasDragged && IsEnabled && legalMovesForHighlight.Contains(targetCoord))
            {
                await OnMove.InvokeAsync(new MoveDto(selectedFrom, targetCoord, PlayerId));
            }
            selectedFrom = null;
            legalMovesForHighlight.Clear();
            _isCurrentlyDragging = false;
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandlePieceDragEnd((string pieceCoord, bool droppedOnTarget) args)
        {
            if (IsSquareSelectionModeActiveForCard)
            {
                _isCurrentlyDragging = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            if (!args.droppedOnTarget)
            {
                selectedFrom = null;
                legalMovesForHighlight.Clear();
            }
            _isCurrentlyDragging = false;
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnSquareClick(string coord)
        {
            if (_isCurrentlyDragging || !IsEnabled || Board?.Squares == null) return;

            if (IsSquareSelectionModeActiveForCard)
            {
                if (HighlightedCardTargetSquaresForSelection != null && HighlightedCardTargetSquaresForSelection.Count > 0)
                {
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
                await OnSquareClickForCard.InvokeAsync(coord);
                // StateHasChanged(); // Nicht hier, da der Handler in Chess.razor.cs dies auslöst
                return;
            }

            (int rank, int file) = PositionHelper.ToIndices(coord);
            PieceDto? pieceOnSquare = (rank >= 0 && rank < 8 && file >= 0 && file < 8) ? Board.Squares[rank][file] : null;

            if (selectedFrom is null)
            {
                if (pieceOnSquare.HasValue && pieceOnSquare.Value.IsOfPlayerColor(MyPlayerColor))
                {
                    selectedFrom = coord;
                    legalMovesForHighlight.Clear();
                    try
                    {
                        legalMovesForHighlight = (await Game.GetLegalMovesAsync(GameId, coord, PlayerId)).ToList();
                    }
                    catch (Exception) { legalMovesForHighlight.Clear(); }
                }
            }
            else
            {
                if (coord == selectedFrom)
                {
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
                else if (legalMovesForHighlight.Contains(coord))
                {
                    await OnMove.InvokeAsync(new MoveDto(selectedFrom, coord, PlayerId));
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
                else if (pieceOnSquare.HasValue && pieceOnSquare.Value.IsOfPlayerColor(MyPlayerColor))
                {
                    selectedFrom = coord;
                    legalMovesForHighlight.Clear();
                    try
                    {
                        legalMovesForHighlight = (await Game.GetLegalMovesAsync(GameId, coord, PlayerId)).ToList();
                    }
                    catch (Exception) { legalMovesForHighlight.Clear(); }
                }
                else
                {
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnParametersSetAsync()
        {
            if (Board?.Squares == null)
            {
                selectedFrom = null;
                legalMovesForHighlight.Clear();
                return;
            }

            if (IsSquareSelectionModeActiveForCard &&
                ((HighlightedCardTargetSquaresForSelection?.Count ?? 0) > 0 || PlayerColorForCardPieceSelection.HasValue)) 
            {
                legalMovesForHighlight.Clear();
                selectedFrom = null;
            }
            else if (selectedFrom != null && !IsSquareSelectionModeActiveForCard)
            {
                bool pieceStillExistsAndIsMine = false;
                (int r, int f) = PositionHelper.ToIndices(selectedFrom);
                if (r >= 0 && r < 8 && f >= 0 && f < 8 && Board.Squares[r][f].HasValue)
                {
                    PieceDto pieceValue = Board.Squares[r][f]!.Value;
                    pieceStillExistsAndIsMine = pieceValue.IsOfPlayerColor(MyPlayerColor);
                }

                if (!pieceStillExistsAndIsMine)
                {
                    selectedFrom = null;
                    legalMovesForHighlight.Clear();
                }
                else
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