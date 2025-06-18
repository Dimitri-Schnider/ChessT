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

namespace ChessClient.Pages.Components.Board
{
    // Die Code-Behind-Klasse für die SquareComponent.
    // Diese Klasse ist von entscheidender Bedeutung, da sie die gesamte Logik für ein einzelnes Feld verwaltet,
    // einschliesslich der komplexen Interaktion mit JavaScript für Drag-and-Drop (DnD) und der dynamischen Anzeige von Highlights.
    public partial class SquareComponent : ComponentBase, IAsyncDisposable
    {
        #region Parameter
        // Von der übergeordneten Komponente (ChessBoard) bereitgestellte Parameter

        [Parameter] public BoardDto Board { get; set; } = null!;    // Das gesamte Brett-Datenobjekt.
        [Parameter] public int Rank { get; set; }                   // Der 0-basierte Zeilen-Index des Feldes (0-7).
        [Parameter] public int File { get; set; }                   // Der 0-basierte Spalten-Index des Feldes (0-7).

        // Highlight-Parameter
        [Parameter] public bool IsHighlightedInternal { get; set; }                     // Für normale legale Züge.
        [Parameter] public bool IsHighlightedForCardPieceSelection { get; set; }        // Für die Auswahl einer Figur bei einem Karteneffekt.
        [Parameter] public bool IsHighlightedForCardTargetSelection { get; set; }       // Für die Auswahl eines leeren Zielfeldes bei einem Karteneffekt (z.B. Wiedergeburt).
        [Parameter] public bool IsFirstSelectedForCardEffect { get; set; }              // Hebt das erste Feld bei einer mehrstufigen Kartenaktion hervor.
        [Parameter] public string? CardEffectHighlightType { get; set; }                // Für direkte Karteneffekte wie Teleport oder Tausch.
        [Parameter] public string? CurrentStrongMoveFrom { get; set; }                  // Startfeld des letzten Zugs (starke Hervorhebung).
        [Parameter] public string? CurrentStrongMoveTo { get; set; }                    // Zielfeld des letzten Zugs (starke Hervorhebung).
        [Parameter] public string? PreviousSequenceMoveFrom { get; set; }               // Startfeld des vorletzten Zugs (für Extrazug-Sequenzen).
        [Parameter] public string? PreviousSequenceMoveTo { get; set; }                 // Zielfeld des vorletzten Zugs (für Extrazug-Sequenzen).
        [Parameter] public bool IsCurrentMoveTheThirdInSequence { get; set; }           // Spezialflag für den zweiten Zug eines "Extrazugs".

        // Interaktions-Parameter
        [Parameter] public EventCallback<string> OnClick { get; set; }                  // Callback für einen normalen Klick (Zugauswahl).
        [Parameter] public EventCallback<string> OnClickForCard { get; set; }           // Callback für einen Klick im Karten-Auswahlmodus.
        [Parameter] public EventCallback<string> OnPieceDragStartInternal { get; set; } // Callback, wenn ein Drag-Vorgang beginnt.
        [Parameter] public EventCallback<string> OnSquareDropInternal { get; set; }     // Callback, wenn eine Figur auf diesem Feld fallengelassen wird.
        [Parameter] public EventCallback<(string pieceCoord, bool droppedOnTarget)> OnPieceDragEndInternal { get; set; } // Callback, wenn ein Drag-Vorgang endet.

        // Zustands-Parameter
        [Parameter] public bool IsBeingDraggedOriginal { get; set; }                    // True, wenn die Figur auf DIESEM Feld gerade gezogen wird.
        [Parameter] public bool IsBoardEnabledOverall { get; set; }                     // Gibt an, ob das Brett insgesamt interaktiv ist.
        [Parameter] public bool CanThisPieceBeDragged { get; set; }                     // Gibt an, ob die spezifische Figur auf diesem Feld ziehbar ist.
        [Parameter] public bool IsSquareSelectionModeActiveForCard { get; set; }        // Zeigt an, ob auf eine Feldauswahl für eine Karte gewartet wird.

        // UI-Parameter
        [Parameter] public bool ShowRankLabel { get; set; }                             // True, um die Rank-Beschriftung (1-8) anzuzeigen.
        [Parameter] public bool ShowFileLabel { get; set; }                             // True, um die File-Beschriftung (a-h) anzuzeigen.

        #endregion

        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;    // Dienst für die Interaktion mit JavaScript.

        // Private Felder
        private DotNetObjectReference<SquareComponent>? dotNetHelper;   // Eine Referenz auf diese C#-Instanz, die an JavaScript übergeben wird.
        private ElementReference pieceImageElementRef;                  // Referenz auf das <img>-Element der Figur.
        private ElementReference squareElementRef;                      // Referenz auf das <div>-Element des Feldes.
        private bool _isPiecePresentLastRender;                         // Merker, ob beim letzten Render eine Figur vorhanden war, um Änderungen zu erkennen.
        private bool _draggableInitialized;                             // Merker, ob die DnD-Funktionalität für die Figur initialisiert wurde.
        private bool _droppableInitialized;                             // Merker, ob die DnD-Funktionalität für das Feld initialisiert wurde.
        private ElementReference _lastInitializedPieceImageRef;         // Speichert die Referenz des letzten Bildes, das "draggable" gemacht wurde.

        // Berechnete Eigenschaften
        private string Coord => PositionHelper.ToAlgebraic(Rank, File);                             // Gibt die Koordinate des Feldes als String zurück (z.B. "e4").
        private string RankDisplay => (8 - Rank).ToString(CultureInfo.InvariantCulture);            // Die anzuzeigende Zahl (1-8).
        private string FileDisplay => ((char)('a' + File)).ToString(CultureInfo.InvariantCulture);  // Der anzuzeigende Buchstabe (a-h).
        private bool IsDarkSquare() => (Rank + File) % 2 != 0;                                      // Bestimmt, ob das Feld dunkel oder hell ist.

        // Stellt die korrekte CSS-Klasse für die Hervorhebung basierend auf einer Prioritätenliste zusammen.
        protected string GetHighlightClass()
        {
            if (IsHighlightedForCardTargetSelection) return "highlight-card-actionable-target";
            if (!string.IsNullOrEmpty(CardEffectHighlightType)) return $"highlight-{CardEffectHighlightType.ToLowerInvariant()}";
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

        // Initialisiert die .NET-Referenz für die JavaScript-Interop.
        protected override void OnInitialized()
        {
            dotNetHelper = DotNetObjectReference.Create(this);
            if (Board?.Squares != null)
            {
                _isPiecePresentLastRender = Board.Squares[Rank][File].HasValue;
            }
        }

        // Drag-and-Drop Lifecycle Management (JS-Interop) 

        // Nach dem Rendern wird die DnD-Funktionalität mit JavaScript initialisiert oder aktualisiert.
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (Board?.Squares == null || JSRuntime == null) return;
            bool currentPiecePresent = Board.Squares[Rank][File].HasValue;

            // Initialisiert das Feld als "droppable" (ein Ort, wo man Figuren ablegen kann).
            if (squareElementRef.Context != null && (firstRender || !_droppableInitialized))
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("chessDnD.initDroppable", squareElementRef, Coord, dotNetHelper);
                    _droppableInitialized = true;
                }
                catch (JSException ex) { Console.WriteLine($"Error init droppable for {Coord}: {ex.Message}"); }
            }

            // Wenn eine Figur vorhanden ist, wird sie als "draggable" initialisiert.
            if (currentPiecePresent && pieceImageElementRef.Context != null)
            {
                // Komplizierte Logik, um sicherzustellen, dass JS-Listener korrekt entfernt und neu hinzugefügt werden, wenn sich die Figur ändert.
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
                    // Stellt sicher, dass der 'draggable'-Zustand in JS immer mit dem C#-Zustand übereinstimmt.
                    await JSRuntime.InvokeVoidAsync("chessDnD.setPieceDraggableState", pieceImageElementRef, CanThisPieceBeDragged && IsBoardEnabledOverall);
                }
            }
            // Wenn keine Figur mehr da ist, werden alte DnD-Listener entfernt.
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

        // Diese Methode wird ebenfalls kontinuierlich aufgerufen und sorgt dafür, dass der JS-Zustand aktuell bleibt.
        protected override async Task OnParametersSetAsync()
        {
            if (Board?.Squares == null || JSRuntime == null) return;
            if (squareElementRef.Context != null && _droppableInitialized)
            {
                try
                {
                    // Aktualisiert, ob auf diesem Feld visuell ein Drop erlaubt ist.
                    bool allowDropHighlight = IsHighlightedInternal && !IsSquareSelectionModeActiveForCard && !IsHighlightedForCardTargetSelection;
                    await JSRuntime.InvokeVoidAsync("chessDnD.setSquareDroppableVisualState", squareElementRef, allowDropHighlight);
                }
                catch (JSException ex) { Console.WriteLine($"Error in OnParametersSetAsync (setSquareDroppableVisualState) for {Coord}: {ex.Message}"); }
            }

            // Die Logik hier ist sehr ähnlich zu OnAfterRenderAsync, um auf Parameter-Änderungen zwischen Render-Zyklen zu reagieren.
            bool currentPiecePresent = Board.Squares[Rank][File].HasValue;
            if (currentPiecePresent && pieceImageElementRef.Context != null)
            {
                if (CanThisPieceBeDragged && IsBoardEnabledOverall && !_draggableInitialized)
                {
                    // Aufräumen alter Listener, falls nötig
                    if (_lastInitializedPieceImageRef.Id != default && _lastInitializedPieceImageRef.Id != pieceImageElementRef.Id && _lastInitializedPieceImageRef.Context != null)
                    {
                        try { await JSRuntime.InvokeVoidAsync("chessDnD.disposeInteractable", _lastInitializedPieceImageRef, Coord, "drag"); }
                        catch (Exception ex) { Console.WriteLine($"Error disposing old draggable in OnParamsSet (before new init) for {Coord}: {ex.Message}"); }
                    }
                    // Neu initialisieren
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

        // JSInvokable Methoden (von JavaScript aufgerufen)

        [JSInvokable]
        public async Task JsOnDragStart(string pieceCoord)
        {
            if (pieceCoord == Coord && !IsSquareSelectionModeActiveForCard)
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

        // Allgemeiner Klick-Handler

        // Dieser Handler entscheidet, welcher übergeordnete Callback ausgelöst wird,
        // basierend darauf, ob das Brett im normalen Modus oder im Karten-Auswahlmodus ist.
        private async Task HandleGeneralClick()
        {
            if (IsSquareSelectionModeActiveForCard)
            {
                await OnClickForCard.InvokeAsync(Coord);
            }
            else
            {
                await OnClick.InvokeAsync(Coord);
            }
        }

        // Räumt alle JS-Interop-Ressourcen und Listener sauber auf, wenn die Komponente zerstört wird.
        public async ValueTask DisposeAsync()
        {
            if (dotNetHelper != null)
            {
                if (JSRuntime != null)
                {
                    if (_draggableInitialized && _lastInitializedPieceImageRef.Context != null)
                    {
                        try { await JSRuntime.InvokeVoidAsync("chessDnD.disposeInteractable", _lastInitializedPieceImageRef, Coord, "drag"); }
                        catch (Exception ex) { Console.WriteLine($"Dispose error draggable {Coord}: {ex.Message}"); }
                    }
                    if (_droppableInitialized && squareElementRef.Context != null)
                    {
                        try { await JSRuntime.InvokeVoidAsync("chessDnD.disposeInteractable", squareElementRef, Coord, "drop"); }
                        catch (Exception ex) { Console.WriteLine($"Dispose error droppable {Coord}: {ex.Message}"); }
                    }
                }
                dotNetHelper.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}