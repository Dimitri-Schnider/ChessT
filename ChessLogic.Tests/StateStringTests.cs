using Xunit;
using ChessLogic;
using ChessLogic.Utilities; 
using ChessLogic.Moves;  

namespace ChessLogic.Tests
{
    public class StateStringTests
    {
        [Fact]
        public void InitialBoardStateStringIsCorrect()
        {
            // Arrange
            Board board = Board.Initial();
            Player currentPlayer = Player.White;

            // Act
            string stateString = new StateString(currentPlayer, board).ToString();

            // Erklärung
            // rnbqkbnr: Das ist die 8.Reihe.Von links(a - Linie) nach rechts(h-Linie): schwarzer Turm, Springer, Läufer, Dame, König, Läufer, Springer, Turm.
            // pppppppp: Das ist die 7.Reihe – acht schwarze Bauern.
            // 8: Die 6.Reihe ist komplett leer.
            // 8: Die 5.Reihe ist komplett leer.
            // 8: Die 4.Reihe ist komplett leer.
            // 8: Die 3.Reihe ist komplett leer.
            // PPPPPPPP: Das ist die 2.Reihe – acht weiße Bauern.
            // RNBQKBNR: Das ist die 1.Reihe – weiße Figuren in derselben Anordnung wie die schwarzen auf der 8.Reihe.

            // Assert
            string expectedFenPrefix = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";
            Assert.Equal(expectedFenPrefix, stateString);
        }

        [Fact]
        public void StateStringAfterE2E4()
        {
            // Arrange
            Board board = Board.Initial(); 
            Player initialPlayer = Player.White; // Bevor der Zug gemacht wird

            Piece? pawn = board[new Position(6, 4)]; 
            Assert.NotNull(pawn); // Sicherstellen, dass der Bauer existiert

            // Definiere den Zug
            Move whitePawnDoubleStep = new DoublePawn(new Position(6, 4), new Position(4, 4)); // e2-e4

            // Führe den Zug auf dem Brett aus (simuliert, was GameState.MakeMove tun würde)
            whitePawnDoubleStep.Execute(board); // Dies setzt auch PawnSkipPosition auf dem Board

            Player currentPlayerAfterMove = initialPlayer.Opponent(); // Nach dem Zug ist Schwarz am Zug

            // Act
            // Übergebe den gerade ausgeführten Zug an den StateString Konstruktor
            string stateString = new StateString(currentPlayerAfterMove, board, whitePawnDoubleStep).ToString();

            // Assert
            string expectedFenPrefix = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3";
            Assert.Equal(expectedFenPrefix, stateString); 
        }

        [Fact]
        public void StateStringWithNoCastlingRights()
        {
            // Arrange
            Board board = Board.Initial();
            Player currentPlayer = Player.White;

            Piece? whiteKing = board[new Position(7, 4)]; if (whiteKing != null) whiteKing.HasMoved = true;
            Piece? whiteRookA = board[new Position(7, 0)]; if (whiteRookA != null) whiteRookA.HasMoved = true;
            Piece? whiteRookH = board[new Position(7, 7)]; if (whiteRookH != null) whiteRookH.HasMoved = true;
            Piece? blackKing = board[new Position(0, 4)]; if (blackKing != null) blackKing.HasMoved = true;
            Piece? blackRookA = board[new Position(0, 0)]; if (blackRookA != null) blackRookA.HasMoved = true;
            Piece? blackRookH = board[new Position(0, 7)]; if (blackRookH != null) blackRookH.HasMoved = true;

            // Act
            string stateString = new StateString(currentPlayer, board).ToString();

            // Assert
            string expectedFenPrefix = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - -";
            Assert.Equal(expectedFenPrefix, stateString);
        }

        [Fact]
        public void StateStringForBlackToMove()
        {
            // Arrange
            Board board = Board.Initial();
            Player currentPlayer = Player.Black;

            // Act
            string stateString = new StateString(currentPlayer, board).ToString();

            // Assert
            string expectedFenPrefix = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq -";
            Assert.Equal(expectedFenPrefix, stateString);
        }

        [Fact]
        public void StateStringAfterSeveralMovesAndBlackCastlesQueenSide()
        {
            // Arrange
            Board board = new Board(); // Leeres Brett zum Start
            Piece? bk = new King(Player.Black);
            Piece? brA_black = new Rook(Player.Black);
            Piece? wk = new King(Player.White);
            Piece? wrH_white = new Rook(Player.White);
            Piece? wrA_white = new Rook(Player.White); // Weißer Turm für a1
            Piece? bpE7 = new Pawn(Player.Black);      // Schwarzer Bauer auf e7
            Piece? wpE4 = new Pawn(Player.White);      // Weißer Bauer auf e4 (simuliert e2-e4)

            // Schwarze Figuren für Rochade und einen Bauern
            board[0, 4] = bk;        // Ke8 (König auf e8)
            board[0, 0] = brA_black; // Ra8 (Turm auf a8)
            board[1, 4] = bpE7;      // Pe7 (Bauer auf e7)

            // Weiße Figuren für Rochaderechte und einen Bauern
            board[7, 4] = wk;        // Ke1 (König auf e1)
            board[7, 0] = wrA_white; // Ra1 (Turm auf a1)
            board[7, 7] = wrH_white; // Rh1 (Turm auf h1)
            board[4, 4] = wpE4;      // Pe4 (Bauer auf e4, simuliert dass er von e2 kam)

            // Sicherstellen, dass relevante Figuren für Rochade als unbewegt gelten
            // und andere Figuren ggf. als bewegt markiert sind.
            if (wpE4 != null) wpE4.HasMoved = true; // Weißer Bauer auf e4 hat bereits gezogen

            if (bk is King kingBk) kingBk.HasMoved = false;
            if (brA_black is Rook rookBrABlack) rookBrABlack.HasMoved = false;

            if (wk is King kingWk) kingWk.HasMoved = false;
            if (wrA_white is Rook rookWraWhite) rookWraWhite.HasMoved = false; // Wichtig für 'Q'
            if (wrH_white is Rook rookWrhWhite) rookWrhWhite.HasMoved = false; // Wichtig für 'K'

            Player currentPlayer = Player.Black; // Schwarz ist am Zug, um zu rochieren

            // Schwarz führt die lange Rochade aus
            // König von e8 (0,4) nach c8 (0,2)
            // Turm von a8 (0,0) nach d8 (0,3)
            Move castleQS = new Castle(MoveType.CastleQS, new Position(0, 4));
            castleQS.Execute(board); // Modifiziert das 'board'-Objekt direkt

            currentPlayer = Player.White; // Nach dem Zug von Schwarz ist Weiß am Zug

            // Act: Generiere den StateString für die aktuelle Brettstellung
            string stateString = new StateString(currentPlayer, board).ToString();

            // Assert
            // Brettstellung nach schwarzer langer Rochade:
            // 8. Reihe (row 0): ..kr....  (schwarzer König auf c8 (0,2), Turm auf d8 (0,3)) -> 2kr4
            // 7. Reihe (row 1): ....p...  (schwarzer Bauer auf e7 (1,4)) -> 4p3
            // 6.-3. R. (row 2-5): leer -> 8/8/8/8
            // 2. Reihe (row 6 -> FEN rank 2): (Hier sollte der weiße Bauer von e4 stehen, falls er nicht geschlagen wurde)
            //    Unser weißer Bauer ist auf e4 (4,4), das ist FEN-Rang 4. FEN-Rang 2 (row 6) ist leer.
            //    Korrektur der Erwartung für die Bauern:
            //    FEN-Rang 4 (row 4): ....P... (weißer Bauer auf e4 (4,4))
            //    FEN-Ränge 3 und 2 (rows 5, 6) sind leer. -> /8/8/
            // 1. Reihe (row 7): R...K..R  (weißer Turm a1 (7,0), König e1 (7,4), Turm h1 (7,7)) -> R3K2R
            // Am Zug: w (Weiß)
            // Rochaderechte: KQ (Weiß kann noch beide. Schwarz hat gerade rochiert, König und Turm haben sich bewegt, also keine Rochaderechte mehr für Schwarz)
            // En Passant: - (kein direkter Doppelschritt des Gegners im vorherigen Zug)
            string expectedFenPrefix = "2kr4/4p3/8/8/4P3/8/8/R3K2R w KQ -"; // Korrigierte Bauernposition und erste Reihe
            Assert.Equal(expectedFenPrefix, stateString);
        }
    }
}