using System.Drawing;

namespace MyOthelloWeb.Models
{
    public class Log
    {
        public List<LogOfGame> LogOfGame { get; private set; } = new List<LogOfGame>();

        public void KeepALogOfGame(Boolean isPass , Turn turn, Point pointToPut)
        {
            this.LogOfGame.Add(new LogOfGame(isPass,turn,pointToPut));
        }
        public void KeepALogOfGame(Turn turn, Point pointToPut)
        {
            this.KeepALogOfGame(false, turn, pointToPut);
        }
        public void EraceLogFromSpecifiedTurn(Int32 numberOfTurn)
        {
            this.LogOfGame.RemoveRange(numberOfTurn, this.LogOfGame.Count - numberOfTurn);
        }
    }

    public class LogOfGame
    {
        public Boolean IsPass { get; }

        public Turn Turn { get; }

        public Point Point { get; }

        public LogOfGame(Boolean isPass, Turn turn, Point point)
        {
            IsPass = isPass;
            Turn = turn;
            Point = point;
        }
    }
}
