using System;
using System.Collections.Generic;

namespace OthelloClassLibrary.Models
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

    //
    public class LogOfGame
    {
        public static LogOfGame Parse(string line) {
            var splitLine = line.Split('@');
            var isPass = splitLine[0] == "True";
            var turn = splitLine[1] == "First" ? Turn.First : Turn.Second;
            var pointText = splitLine[2];
            var (x, y) = pointText.StartsWith("-")
                ? (pointText.Substring(0, 2), pointText.Substring(2, 2))
                : (pointText.Substring(0, 1), pointText.Substring(1, 1));
            var point = new Point(Int32.Parse(x), Int32.Parse(y));
            return new LogOfGame(isPass, turn, point);
        }

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
