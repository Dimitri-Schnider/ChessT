using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAnalysis
{
    public class ChessGame
    {
        public string gameId { get; set;}
        public string playerWhiteName{ get; set;}
        public string playerWhiteID { get; set;}
        public string playerBalckName { get; set;}
        public string playerBlackID { get; set;}
        public int initialTime { get; set;}

        public string whoIsWinner { get; set;}
        public string reasonforGameEnd{ get; set;}
        public string timeStampStart { get; set;}
        public string timeStampEnd { get; set;}

    }
    
    public class ChessMove
    {
        public int moveNumber { get; set; }
        public string playerId { get; set; }
        public int playerColor { get; set; }
        public string movedFrom { get; set; }
        public string movedTo { get; set; }
        public int actualMoveType { get; set; }
        public string promotionPiece { get; set; }
        public string timeStampUtc { get; set; }
        public string timeTaken { get; set; }
        public string remainingTimeWhite { get; set; }
        public string RemainingTimeBlack { get; set; }
        public string pieceMoved { get; set; }
        public string capturedPiece { get; set;}
           
    
    
    }
}
