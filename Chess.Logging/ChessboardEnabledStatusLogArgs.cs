// File: [SolutionDir]\Chess.Logging\ChessboardEnabledStatusLogArgs.cs
// (Neue Datei oder in einer existierenden passenden Datei im Chess.Logging Projekt)
namespace Chess.Logging
{
    // KORREKTUR: von internal zu public readonly struct
    public readonly struct ChessboardEnabledStatusLogArgs
    {
        public bool ModalStateNull { get; }
        public bool CardStateNull { get; }
        public bool GameCoreStateNull { get; }
        public bool HighlightStateNull { get; }
        public bool IsAwaitingTurnConfirmation { get; }
        public bool ShowCreateGameModal { get; }
        public bool ShowJoinGameModal { get; }
        public bool ShowPieceSelectionModal { get; }
        public bool ShowCardInfoPanelModal { get; }
        public string? ActiveCardForBoardSelectionId { get; }
        public bool IsRebirthAwaitingSquareSelection { get; }

        public ChessboardEnabledStatusLogArgs(
            bool modalStateNull, bool cardStateNull, bool gameCoreStateNull, bool highlightStateNull,
            bool isAwaitingTurnConfirmation, bool showCreateGameModal, bool showJoinGameModal,
            bool showPieceSelectionModal, bool showCardInfoPanelModal,
            string? activeCardForBoardSelectionId, bool isRebirthAwaitingSquareSelection)
        {
            ModalStateNull = modalStateNull;
            CardStateNull = cardStateNull;
            GameCoreStateNull = gameCoreStateNull;
            HighlightStateNull = highlightStateNull;
            IsAwaitingTurnConfirmation = isAwaitingTurnConfirmation;
            ShowCreateGameModal = showCreateGameModal;
            ShowJoinGameModal = showJoinGameModal;
            ShowPieceSelectionModal = showPieceSelectionModal;
            ShowCardInfoPanelModal = showCardInfoPanelModal;
            ActiveCardForBoardSelectionId = activeCardForBoardSelectionId;
            IsRebirthAwaitingSquareSelection = isRebirthAwaitingSquareSelection;
        }

        public override string ToString()
        {
            return $"ModalStateNull={ModalStateNull}, CardStateNull={CardStateNull}, GameCoreStateNull={GameCoreStateNull}, HighlightStateNull={HighlightStateNull}, IsAwaitingTurnConfirmation={IsAwaitingTurnConfirmation}, ShowCreateGameModal={ShowCreateGameModal}, ShowJoinGameModal={ShowJoinGameModal}, ShowPieceSelectionModal={ShowPieceSelectionModal}, ShowCardInfoPanelModal={ShowCardInfoPanelModal}, ActiveCardForBoardSelectionId={ActiveCardForBoardSelectionId ?? "null"}, IsRebirthAwaitingSquareSelection={IsRebirthAwaitingSquareSelection}";
        }
    }


}