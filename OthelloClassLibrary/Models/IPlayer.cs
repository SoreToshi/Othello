using System;
using System.Threading.Tasks;

namespace OthelloClassLibrary.Models
{
    public interface IPlayer
    { 
        Boolean IsAutomaton { get; }

        Turn Turn { get; }

        Task<Point> RequestPutPiece(OthelloBoard othelloBoard);
    }
}
