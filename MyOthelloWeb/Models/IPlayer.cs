namespace MyOthelloWeb.Models
{
    public interface IPlayer
    {
        Boolean IsAutomaton { get; }

        Turn Turn { get; }

        Task<Point> RequestPutPiece(OthelloBoard othelloBoard);
    }
}
