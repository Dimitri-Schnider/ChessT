using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace ChessLogic.Tests
{
    // Testklasse für die spezifische Zuglogik des Bauern
    public class PawnTests
    {
        // Testfall: Weißer Bauer kann von Startposition einen oder zwei Schritte vorwärts ziehen
        [Fact]
        public void WhitePawnInitialForwardMoves()
        {
            // Arrange
            Board board = new Board();
            Pawn whitePawn = new Pawn(Player.White);
            Position pawnPos = new Position(6, 4); // e2
            board[pawnPos] = whitePawn;

            // Act
            IEnumerable<Move> moves = whitePawn.GetMoves(pawnPos, board);

            // Assert
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(5, 4)) && m.Type == MoveType.Normal);     // e3
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(4, 4)) && m.Type == MoveType.DoublePawn); // e4
            Assert.Equal(2, moves.Count()); // Genau diese zwei Züge
        }

        // Testfall: Weißer Bauer kann nach dem ersten Zug nur noch einen Schritt vorwärts
        [Fact]
        public void WhitePawnForwardMoveAfterInitial()
        {
            // Arrange
            Board board = new Board();
            Pawn whitePawn = new Pawn(Player.White);
            whitePawn.HasMoved = true; // Simuliert, dass der Bauer schon gezogen hat
            Position pawnPos = new Position(5, 4); // e3
            board[pawnPos] = whitePawn;

            // Act
            IEnumerable<Move> moves = whitePawn.GetMoves(pawnPos, board);

            // Assert
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(4, 4)) && m.Type == MoveType.Normal); // e4
            Assert.Single(moves); // Nur dieser eine Zug
        }

        // Testfall: Weißer Bauer generiert Umwandlungszüge, wenn er die letzte Reihe erreicht
        [Fact]
        public void WhitePawnGeneratesPromotionMovesOnLastRank()
        {
            // Arrange
            Board board = new Board();
            Pawn whitePawn = new Pawn(Player.White);
            Position pawnPos = new Position(1, 4); // e7 (ein Feld vor der Umwandlung)
            board[pawnPos] = whitePawn;

            // Act
            IEnumerable<Move> moves = whitePawn.GetMoves(pawnPos, board);
            Position promotionSquare = new Position(0, 4); // e8

            // Assert
            // Es sollten 4 Umwandlungszüge angeboten werden (Dame, Turm, Läufer, Springer)
            Assert.Contains(moves, m => m.ToPos.Equals(promotionSquare) && m is PawnPromotion promo && promo.PromotionTo == PieceType.Queen);
            Assert.Contains(moves, m => m.ToPos.Equals(promotionSquare) && m is PawnPromotion promo && promo.PromotionTo == PieceType.Rook);
            Assert.Contains(moves, m => m.ToPos.Equals(promotionSquare) && m is PawnPromotion promo && promo.PromotionTo == PieceType.Bishop);
            Assert.Contains(moves, m => m.ToPos.Equals(promotionSquare) && m is PawnPromotion promo && promo.PromotionTo == PieceType.Knight);
            Assert.Equal(4, moves.Count(m => m.ToPos.Equals(promotionSquare) && m is PawnPromotion));
        }

        // Testfall: Weißer Bauer kann diagonal schlagen (und macht keinen Doppelschritt von d5)
        [Fact]
        public void WhitePawnDiagonalCapture()
        {
            // Arrange
            Board board = new Board();
            Pawn whitePawn = new Pawn(Player.White);
            whitePawn.HasMoved = true; // WICHTIG: Bauer auf d5 hat bereits gezogen
            Position pawnPos = new Position(3, 3); // d5
            board[pawnPos] = whitePawn;

            board[new Position(2, 2)] = new Pawn(Player.Black); // Schwarzer Bauer auf c6
            board[new Position(2, 4)] = new Rook(Player.Black); // Schwarzer Turm auf e6

            // Act
            IEnumerable<Move> moves = whitePawn.GetMoves(pawnPos, board);

            // Assert
            // Erwartet einen normalen Vorwärtszug und zwei Schlagzüge
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 3)) && m.Type == MoveType.Normal); // d6 (Vorwärtszug)
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 2)) && m.Type == MoveType.Normal); // Schlag auf c6
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 4)) && m.Type == MoveType.Normal); // Schlag auf e6

            // Es sollte keinen DoublePawn-Zug mehr geben
            Assert.DoesNotContain(moves, m => m.Type == MoveType.DoublePawn);

            Assert.Equal(3, moves.Count()); // Jetzt sollte die Anzahl stimmen
        }

        // Testfall: Vorbereitung für En Passant (Doppelschritt setzt pawnSkipPosition)
        [Fact]
        public void DoublePawnMoveCorrectlySetsPawnSkipPosition()
        {
            // Arrange
            Board board = new Board();
            Pawn whitePawn = new Pawn(Player.White);
            Position fromPos = new Position(6, 4); // e2
            board[fromPos] = whitePawn;

            Move doublePawnMove = new DoublePawn(fromPos, new Position(4, 4)); // e2-e4

            // Act
            doublePawnMove.Execute(board); // Führe den Doppelschritt aus

            // Assert
            Position expectedSkipPos = new Position(5, 4); // e3 muss die Sprungposition sein
            Assert.Equal(expectedSkipPos, board.GetPawnSkipPosition(Player.White));
            Assert.Null(board.GetPawnSkipPosition(Player.Black)); // Für Schwarz sollte keine gesetzt sein
        }

        // Testfall: Bauer kann En Passant schlagen
        [Fact]
        public void PawnCanPerformEnPassantCapture()
        {
            // Arrange
            Board board = new Board();
            Pawn whitePawnToMove = new Pawn(Player.White); // Der schlagende Bauer
            Position whitePawnPos = new Position(3, 3);    // d5
            board[whitePawnPos] = whitePawnToMove;

            Pawn blackPawnToSkip = new Pawn(Player.Black); // Der Bauer, der den Doppelschritt macht
            Position blackPawnInitialPos = new Position(1, 2); // c7
            board[blackPawnInitialPos] = blackPawnToSkip;

            // Simuliere den Doppelschritt des schwarzen Bauern von c7 nach c5
            Move blackDoubleStep = new DoublePawn(blackPawnInitialPos, new Position(3, 2)); // c7-c5
            blackDoubleStep.Execute(board);
            // Jetzt ist board.GetPawnSkipPosition(Player.Black) == new Position(2,2) (c6)

            // Act: Hole die Züge für den weißen Bauern auf d5
            IEnumerable<Move> whitePawnMoves = whitePawnToMove.GetMoves(whitePawnPos, board);

            // Assert
            // Der weiße Bauer auf d5 sollte nach c6 (En Passant) ziehen können
            Assert.Contains(whitePawnMoves, m => m.ToPos.Equals(new Position(2, 2)) && m.Type == MoveType.EnPassant);
        }
    }
}