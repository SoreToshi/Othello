using System;
using System.Threading.Tasks;

namespace OthelloClassLibrary.Models
{
    public class Human : IPlayer
    {
        public Boolean IsAutomaton { get; } = false;

        public Turn Turn { get; private set; }

        public Human(Turn turn)
        {
            this.Turn = turn;
        }
        public Task<Point> RequestPutPiece(OthelloBoard othelloBoard)
        {
            var tcs = new TaskCompletionSource<Point>();
            tcs.SetResult(null);
            return tcs.Task;
        }
    }
}
