using System;
using System.Collections.Generic;

namespace OthelloClassLibrary.Models
{
    public class Log
    {
        public List<LogOfGame> LogOfGame { get; private set; } = new List<LogOfGame>();

        public Log() {}

        public Log(MyOthelloModel othello) {
            if (othello.Log.LogOfGame.Count != 0)
            {
                foreach (var log in othello.Log.LogOfGame)
                {
                    this.KeepALogOfGame(log.IsPass, log.Turn, log.Point);
                }
            }

            // セレクトサイド状態の時に-8,-8を入れることで受け取ったクライアントがSelectSide状態であることを反映できます。
            if (othello.GameState == GameState.SelectSide)
            {
                this.KeepALogOfGame(othello.Turn, new Point(-8, -8));
            }

            // マッチリタイア状態の時にLogのPointにxに-5を入れることで受け取ったクライアントがリタイア状態であることを、yに-1,-2を入れることで
            // 先手と後手どちらがリタイアしたかを反映できます。
            if (othello.GameState == GameState.MatchRetired)
            {
                var retiredTurnNumber = othello.RetiredTurn == Turn.First ? -1 : -2;
                this.KeepALogOfGame(othello.Turn, new Point(-5, retiredTurnNumber));
            }
        }

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
