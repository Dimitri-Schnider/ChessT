using ChessClient.Extensions;
using ChessClient.Utils;
using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace ChessClient.Pages.Components
{
    public partial class SquareComponent : ComponentBase, IAsyncDisposable
    {
        [Parameter] public BoardDto Board { get; set; } = null!;
        [Parameter] public int Rank { get; set; }
        [Parameter] public int File { get; set; }
        [Parameter] public bool IsHighlightedInternal { get; set; } // Für legale Züge
        [Parameter] public bool IsHighlightedForCardPieceSelection { get; set; } // Für Karteneffekt: Auswahl einer Figur
        [Parameter] public bool IsHighlightedForCardTargetSelection { get; set; } // NEU: Für Karteneffekt: Auswahl eines leeren Zielfeldes (z.B. Rebirth)
        [Parameter] public bool IsFirstSelectedForCardEffect { get; set; }
        [Parameter] public EventCallback<string> OnClick { get; set; }
        [Parameter] public EventCallback<string> OnClickForCard { get; set; }
        [Parameter] public bool IsSquareSelectionModeActiveForCard { get; set; }
        [Parameter] public EventCallback<string> OnPieceDragStartInternal { get; set; }
        [Parameter] public EventCallback<string> OnSquareDropInternal { get; set; }
        [Parameter] public EventCallback<(string pieceCoord, bool droppedOnTarget)> OnPieceDragEndInternal { get; set; }
        [Parameter] public bool IsBeingDraggedOriginal { get; set; }
        [Parameter] public bool IsBoardEnabledOverall { get; set; }
        [Parameter] public bool CanThisPieceBeDragged { get; set; }

        [Parameter] public string? CardEffectHighlightType { get; set; } // Für Teleport, Swap (direkte Effekte)
        [Parameter] public string? CurrentStrongMoveFrom { get; set; }
        [Parameter] public string? CurrentStrongMoveTo { get; set; }
        [Parameter] public string? PreviousSequenceMoveFrom { get; set; }
        [Parameter] public string? PreviousSequenceMoveTo { get; set; }
        [Parameter] public bool IsCurrentMoveTheThirdInSequence { get; set; }
        [Parameter] public bool ShowRankLabel { get; set; }
        [Parameter] public bool ShowFileLabel { get; set; }

        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

        private DotNetObjectReference<SquareComponent>? dotNetHelper;
        private ElementReference pieceImageElementRef;
        private ElementReference squareElementRef;
        private string Coord => PositionHelper.ToAlgebraic(Rank, File);
        private bool _isPiecePresentLastRender;
        private bool _draggableInitialized;
        private bool _droppableInitialized;
        private ElementReference _lastInitializedPieceImageRef;
        private string RankDisplay => (8 - Rank).ToString(CultureInfo.InvariantCulture);
        private string FileDisplay => ((char)('a' + File)).ToString(CultureInfo.InvariantCulture);
        private bool IsDarkSquare() => (Rank + File) % 2 != 0; 



        protected string GetHighlightClass()
        {
            if (IsHighlightedForCardTargetSelection) return "highlight-card-actionable-target";
            if (!string.IsNullOrEmpty(CardEffectHighlightType))
            {
                return $"highlight-{CardEffectHighlightType.ToLowerInvariant()}";
            }
            if (IsCurrentMoveTheThirdInSequence)
            {
                if (CurrentStrongMoveTo == Coord) return "highlight-last-move-to-weaker";
                if (CurrentStrongMoveFrom == Coord) return "highlight-last-move-from-weaker";
            }
            else
            {
                if (CurrentStrongMoveTo == Coord) return "highlight-last-move-to-strong";
                if (CurrentStrongMoveFrom == Coord) return "highlight-last-move-from-strong";
            }
            if (PreviousSequenceMoveTo == Coord) return "highlight-last-move-to-strong";
            if (PreviousSequenceMoveFrom == Coord) return "highlight-last-move-from-strong";
            if (IsFirstSelectedForCardEffect) return "highlight-card-first-selection";
            if (IsHighlightedForCardPieceSelection) return "highlight-card-piece-selection";
            if (IsHighlightedInternal) return "highlight";
            return "";
        }
        // ... Rest der Klasse bleibt gleich
        protected override void OnInitialized()
        {
            dotNetHelper = DotNetObjectReference.Create(this);
            if (Board?.Squares != null)
            {
                _isPiecePresentLastRender = Board.Squares[Rank][File].HasValue;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (Board?.Squares == null || JSRuntime == null) return;
            bool currentPiecePresent = Board.Squares[Rank][File].HasValue;

            if (squareElementRef.Context != null && (firstRender || !_droppableInitialized))
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("chessDnD.initDroppable", squareElementRef, Coord, dotNetHelper);
                    _droppableInitialized = true;
                }
                catch (JSException ex) { Console.WriteLine($"Error init droppable for {Coord}: {ex.Message}"); }
            }

            if (currentPiecePresent && pieceImageElementRef.Context != null)
            {
                if (_draggableInitialized && _lastInitializedPieceImageRef.Id != default && _lastInitializedPieceImageRef.Id != pieceImageElementRef.Id)
                {
                    if (_lastInitializedPieceImageRef.Context != null)
                    {
                        try { await JSRuntime.InvokeVoidAsync("chessDnD.disposeInteractable", _lastInitializedPieceImageRef, Coord, "drag"); }
                        catch (Exception ex) { Console.WriteLine($"Error disposing old draggable for {Coord} in OnAfterRender: {ex.Message}"); }
                    }
                    _draggableInitialized = false;
                }
                if (!_draggableInitialized && CanThisPieceBeDragged && IsBoardEnabledOverall)
                {
                    try
                    {
                        await JSRuntime.InvokeVoidAsync("chessDnD.initDraggable", pieceImageElementRef, Coord, dotNetHelper);
                        _draggableInitialized = true;
                        _lastInitializedPieceImageRef = pieceImageElementRef;
                    }
                    catch (JSException ex) { Console.WriteLine($"Error init draggable for {Coord} in OnAfterRender: {ex.Message}"); }
                }
                if (_draggableInitialized)
                {
                    await JSRuntime.InvokeVoidAsync("chessDnD.setPieceDraggableState", pieceImageElementRef, CanThisPieceBeDragged && IsBoardEnabledOverall);
                }
            }
            else if (!currentPiecePresent && _draggableInitialized)
            {
                if (_lastInitializedPieceImageRef.Context != null)
                {
                    try { await JSRuntime.InvokeVoidAsync("chessDnD.disposeInteractable", _lastInitializedPieceImageRef, Coord, "drag"); }
                    catch (JSException ex) { Console.WriteLine($"Error disposing draggable for {Coord} on piece removal in OnAfterRender: {ex.Message}"); }
                }
                _draggableInitialized = false;
                _lastInitializedPieceImageRef = default;
            }
            _isPiecePresentLastRender = currentPiecePresent;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (Board?.Squares == null || JSRuntime == null) return;
            if (squareElementRef.Context != null && _droppableInitialized)
            {
                try
                {
                    // Die Droppable Visual State Logik muss die neue IsHighlightedForCardTargetSelection berücksichtigen.
                    // Wenn ein Feld ein explizites Ziel für eine Karte ist (z.B. Rebirth), sollte es nicht als normales Drop-Ziel erscheinen,
                    // es sei denn, die Logik für das Droppen ist dieselbe (Klick).
                    bool allowDropHighlight = IsHighlightedInternal && !IsSquareSelectionModeActiveForCard && !IsHighlightedForCardTargetSelection;
                    await JSRuntime.InvokeVoidAsync("chessDnD.setSquareDroppableVisualState", squareElementRef, allowDropHighlight);
                }
                catch (JSException ex) { Console.WriteLine($"Error in OnParametersSetAsync (setSquareDroppableVisualState) for {Coord}: {ex.Message}"); }
            }

            bool currentPiecePresent = Board.Squares[Rank][File].HasValue;
            if (currentPiecePresent && pieceImageElementRef.Context != null)
            {
                if (CanThisPieceBeDragged && IsBoardEnabledOverall && !_draggableInitialized)
                {
                    if (_lastInitializedPieceImageRef.Id != default && _lastInitializedPieceImageRef.Id != pieceImageElementRef.Id && _lastInitializedPieceImageRef.Context != null)
                    {
                        try { await JSRuntime.InvokeVoidAsync("chessDnD.disposeInteractable", _lastInitializedPieceImageRef, Coord, "drag"); }
                        catch (Exception ex) { Console.WriteLine($"Error disposing old draggable in OnParamsSet (before new init) for {Coord}: {ex.Message}"); }
                    }
                    try
                    {
                        await JSRuntime.InvokeVoidAsync("chessDnD.initDraggable", pieceImageElementRef, Coord, dotNetHelper);
                        _draggableInitialized = true;
                        _lastInitializedPieceImageRef = pieceImageElementRef;
                    }
                    catch (JSException ex) { Console.WriteLine($"Error init draggable for {Coord} in OnParametersSetAsync: {ex.Message}"); }
                }
                if (_draggableInitialized)
                {
                    if (pieceImageElementRef.Context != null)
                    {
                        try
                        {
                            await JSRuntime.InvokeVoidAsync("chessDnD.setPieceDraggableState", pieceImageElementRef, CanThisPieceBeDragged && IsBoardEnabledOverall);
                        }
                        catch (JSException ex) { Console.WriteLine($"Error in OnParametersSetAsync (setPieceDraggableState) for {Coord}: {ex.Message}"); }
                    }
                }
            }
            else if (!currentPiecePresent && _draggableInitialized)
            {
                if (_lastInitializedPieceImageRef.Context != null)
                {
                    try { await JSRuntime.InvokeVoidAsync("chessDnD.disposeInteractable", _lastInitializedPieceImageRef, Coord, "drag"); }
                    catch (JSException ex) { Console.WriteLine($"Error disposing draggable for {Coord} on piece removal in OnParamsSet: {ex.Message}"); }
                }
                _draggableInitialized = false;
                _lastInitializedPieceImageRef = default;
            }
        }

        [JSInvokable]
        public async Task JsOnDragStart(string pieceCoord)
        {
            if (pieceCoord == Coord && !IsSquareSelectionModeActiveForCard) // Dragging nur wenn kein Kartenauswahlmodus aktiv
            {
                await OnPieceDragStartInternal.InvokeAsync(pieceCoord);
            }
        }

        [JSInvokable]
        public async Task JsOnDragEnd(string pieceCoord, bool droppedOnValidTarget)
        {
            if (pieceCoord == Coord && !IsSquareSelectionModeActiveForCard)
            {
                await OnPieceDragEndInternal.InvokeAsync((pieceCoord, droppedOnValidTarget));
            }
        }

        [JSInvokable]
        public async Task JsOnDrop(string draggedPieceCoord, string targetSquareCoord)
        {
            if (targetSquareCoord == Coord && !IsSquareSelectionModeActiveForCard)
            {
                await OnSquareDropInternal.InvokeAsync(draggedPieceCoord);
            }
        }

        private async Task HandleGeneralClick()
        {
            // Wenn ein Kartenauswahlmodus aktiv ist (egal ob Figurenauswahl oder Zielfeldauswahl)
            if (IsSquareSelectionModeActiveForCard)
            {
                await OnClickForCard.InvokeAsync(Coord);
            }
            else // Normaler Spielzug-Klick
            {
                await OnClick.InvokeAsync(Coord);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (dotNetHelper != null)
            {
                if (JSRuntime != null)
                {
                    if (_draggableInitialized && _lastInitializedPieceImageRef.Context != null)
                    {
                        try
                        {
                            await JSRuntime.InvokeVoidAsync("chessDnD.disposeInteractable", _lastInitializedPieceImageRef, Coord, "drag");
                        }
                        catch (Exception ex) { Console.WriteLine($"Dispose error draggable {Coord}: {ex.Message}"); }
                    }
                    if (_droppableInitialized && squareElementRef.Context != null)
                    {
                        try
                        {
                            await JSRuntime.InvokeVoidAsync("chessDnD.disposeInteractable", squareElementRef, Coord, "drop");
                        }
                        catch (Exception ex) { Console.WriteLine($"Dispose error droppable {Coord}: {ex.Message}"); }
                    }
                }
                dotNetHelper.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}