using System.Drawing;
using DeepCopy;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace OthelloClassLibrary.Models
{
    public class Cpu : IPlayer
    {
        private enum PointStatus { Clear, Add, AsIs }

        public Boolean IsAutomaton { get; } = true;
        public Turn Turn { get; private set; }

        private Side Side
        {
            get
            {
                return MyOthelloModel.GetSide(this.Turn);
            }
        }

        private List<Point> Score100PointList = new List<Point>();
        private IList<Point> Score20PointList = new List<Point>();
        private List<Point> ScoreMinus50PointList = new List<Point>();
        private IList<Point> ScoreMinus10PointList = new List<Point>();

        public Cpu(Turn turn)
        {
            this.Turn = turn;

            this.Score100PointList.AddRange(new List<Point> { new Point(0, 0), new Point(7, 0), new Point(0, 7), new Point(7, 7) });
            foreach (var num in new[] { 0, 7 })
            {
                for (var i = 1; i < 7; i++)
                {
                    this.Score20PointList.Add(new Point(i, num));
                    this.Score20PointList.Add(new Point(num, i));
                }
            }
            this.ScoreMinus50PointList.AddRange(new List<Point> { new Point(1, 1), new Point(6, 1), new Point(1, 6), new Point(6, 6) });
            foreach (var num in new[] { 1, 6 })
            {
                for (var i = 2; i < 6; i++)
                {
                    this.ScoreMinus10PointList.Add(new Point(i, num));
                    this.ScoreMinus10PointList.Add(new Point(num, i));
                }
            }
        }
        public async Task<Point> RequestPutPiece(OthelloBoard othelloBoard)
        {
            await Task.Delay(1000); // millisec

            var squareNumberListCanBePutAtFirstNode = othelloBoard.FindSquareNumberListCanBePut(this.Side);
            var bestPointList = this.FindBestPointList(othelloBoard, squareNumberListCanBePutAtFirstNode);

            return this.SelectBestPoint(bestPointList);
        }

        private IList<Point> FindBestPointList(OthelloBoard othelloBoard, IList<Int32> squareNumberListCanBePutAtFirstNode)
        {
            if (squareNumberListCanBePutAtFirstNode.Count == 0)
            {
                return new List<Point>();
            }

            var puttablePointCollection = squareNumberListCanBePutAtFirstNode.Select(othelloBoard.SquareNumberToPoint);
            var firstPuttablePoint = puttablePointCollection.First();
            var bestPointList = new List<Point> { firstPuttablePoint };

            var minScore = this.FindFirstRunMinScore(othelloBoard, firstPuttablePoint);

            foreach (var puttablePoint in puttablePointCollection.Skip(1))
            {
                var pointStatus = this.FindPointStatusOfRun(othelloBoard, puttablePoint, minScore);
                if (pointStatus == PointStatus.Clear)
                {
                    bestPointList.Clear();
                }

                if (pointStatus == PointStatus.Add || pointStatus == PointStatus.Clear)
                {
                    bestPointList.Add(puttablePoint);
                }

                minScore = this.FindMinScoreOfRun(othelloBoard, puttablePoint, minScore);
            }
            return bestPointList;
        }

        private Point SelectBestPoint(IList<Point> bestPointList)
        {
            var rand = new Random();
            var randomSquareNumber = rand.Next(bestPointList.Count);
            var bestPoint = bestPointList[randomSquareNumber];
            return bestPoint;
        }

        private Int32 FindFirstRunMinScore(OthelloBoard othelloBoard, Point firstPuttablePoint)
        {
            var side = this.Side;
            var copiedPuttedBoard = this.CreatePuttedBoard(othelloBoard, firstPuttablePoint, side);

            return this.IsPass(copiedPuttedBoard)
                ? this.FindFirstRunPassedMinScore(othelloBoard, firstPuttablePoint)
                : this.FindFirstRunNonPassedMinScore(othelloBoard, firstPuttablePoint);
        }

        private Int32 FindMinScoreOfRun(OthelloBoard othelloBoard, Point puttablePoint, Int32 minScore)
        {
            var side = this.Side;
            var copiedPuttedBoard = this.CreatePuttedBoard(othelloBoard, puttablePoint, side);

            return this.IsPass(copiedPuttedBoard)
                ? this.FindPassedMinScoreOfRun(othelloBoard, puttablePoint, minScore)
                : this.FindNonPassedMinScoreOfRun(othelloBoard, puttablePoint, minScore);
        }

        private PointStatus FindPointStatusOfRun(OthelloBoard othelloBoard, Point puttablePoint, Int32 minScore)
        {
            var side = this.Side;
            var copiedPuttedBoard = this.CreatePuttedBoard(othelloBoard, puttablePoint, side);

            return this.IsPass(copiedPuttedBoard)
                ? this.FindPassedPointStatusOfRun(othelloBoard, puttablePoint, minScore)
                : this.FindNonPassedPointStatusOfRun(othelloBoard, puttablePoint, minScore);
        }

        private Int32 FindFirstRunPassedMinScore(OthelloBoard othelloBoard, Point firstPuttablePoint)
        {
            return this.CalculateScore(othelloBoard, firstPuttablePoint, this.Side);
        }

        private Int32 FindFirstRunNonPassedMinScore(OthelloBoard othelloBoard, Point firstPuttablePoint)
        {
            var minScore = Int32.MaxValue;
            var side = this.Side;
            var secondSide = side.ReverseSide();

            var situationScore = this.CalculateScore(othelloBoard, firstPuttablePoint, side);

            var copiedPuttedBoard = this.CreatePuttedBoard(othelloBoard, firstPuttablePoint, side);

            var secoundSidePuttablePointCollection = copiedPuttedBoard.FindSquareNumberListCanBePut(secondSide).Select(copiedPuttedBoard.SquareNumberToPoint);
            foreach (var secondSidePuttablePoint in secoundSidePuttablePointCollection)
            {
                var secondSideSituationScore = situationScore + this.CalculateScore(copiedPuttedBoard, secondSidePuttablePoint, secondSide);

                minScore = Math.Min(minScore, secondSideSituationScore);
            }
            return minScore;
        }

        private Int32 FindPassedMinScoreOfRun(OthelloBoard othelloBoard, Point puttablePoint, Int32 minScore)
        {
            var situationScore = this.CalculateScore(othelloBoard, puttablePoint, this.Side);

            return minScore < situationScore ? minScore : situationScore;
        }

        private Int32 FindNonPassedMinScoreOfRun(OthelloBoard othelloBoard, Point placeToPut, Int32 minScore)
        {
            var side = this.Side;
            var secondSide = side.ReverseSide();
            var maxScore = Int32.MaxValue;

            var situationScore = this.CalculateScore(othelloBoard, placeToPut, side);

            var copiedPuttedBoard = this.CreatePuttedBoard(othelloBoard, placeToPut, side);

            var secoundSidePuttablePointCollection = copiedPuttedBoard.FindSquareNumberListCanBePut(secondSide).Select(copiedPuttedBoard.SquareNumberToPoint);
            foreach (var secondSidePuttablePoint in secoundSidePuttablePointCollection)
            {
                var secondSideSituationScore = situationScore + this.CalculateScore(copiedPuttedBoard, secondSidePuttablePoint, secondSide);
                // より低いスコアが出た場合minScoreは変わらない
                if (minScore > secondSideSituationScore)
                {
                    return minScore;
                }

                maxScore = Math.Min(maxScore, secondSideSituationScore);
            }
            return minScore == maxScore ? minScore : maxScore;
        }

        private PointStatus FindPassedPointStatusOfRun(OthelloBoard othelloBoard, Point puttablePoint, Int32 minScore)
        {
            var side = this.Side;
            var situationScore = this.CalculateScore(othelloBoard, puttablePoint, side);
            // より低いスコアが出た場合このpointが加わることはない
            if (minScore > situationScore)
            {
                return PointStatus.AsIs;
            }
            return minScore == situationScore ? PointStatus.Add : PointStatus.Clear;
        }
        private PointStatus FindNonPassedPointStatusOfRun(OthelloBoard othelloBoard, Point puttablePoint, Int32 minScore)
        {
            var side = this.Side;
            var secondSide = this.Side.ReverseSide();
            var maxScore = Int32.MaxValue;

            var situationScore = this.CalculateScore(othelloBoard, puttablePoint, side);

            var copiedBoard = this.CreatePuttedBoard(othelloBoard, puttablePoint, side);

            var secoundSidePuttablePointCollection = copiedBoard.FindSquareNumberListCanBePut(secondSide).Select(copiedBoard.SquareNumberToPoint);
            foreach (var secoundSidePuttablePoint in secoundSidePuttablePointCollection)
            {
                var secondSideSituationScore = situationScore + this.CalculateScore(copiedBoard, secoundSidePuttablePoint, secondSide);
                // より低いスコアが出た場合このpointが加わることはない
                if (minScore > secondSideSituationScore)
                {
                    return PointStatus.AsIs;
                }

                maxScore = Math.Min(maxScore, secondSideSituationScore);
            }
            return minScore == maxScore ? PointStatus.Add : PointStatus.Clear;
        }

        private Int32 CalculateScore(OthelloBoard board, Point pointToPut, Side side)
        {
            var copiedBoard = DeepCopier.Copy(board);
            var (beforePutCount, afterPutCount) = copiedBoard.PutPiece(pointToPut, side);
            // 自分で置いた石は除外します。
            var turnOverCount = (afterPutCount - 1) - beforePutCount;
            var score = this.PointToScore(pointToPut);
            var NumberOfPuttedPiece = copiedBoard.CountOneSidePiece(Side.White) + copiedBoard.CountOneSidePiece(Side.Black);
            if (NumberOfPuttedPiece <= 16)
            {
                return this.Side == side
                    ? score - turnOverCount // 序盤、自分のひっくり返した枚数は少ないほうがいいため
                    : turnOverCount - score; // 序盤、相手のひっくり返した枚数が多いほうがいいため
            }
            else
            {
                return this.Side == side
                    ? score + turnOverCount // 中盤、終盤自分のひっくり返した枚数は多いいほうがいいため
                    : -1 * turnOverCount - score; // 中盤、終盤相手のひっくり返した枚数が少ないほうがいいため
            }
        }

        private Boolean IsPass(OthelloBoard othelloBoard)
        {
            var secondSide = this.Side.ReverseSide();
            return othelloBoard.HasTherePlaceToPut(secondSide) == false;
        }

        private OthelloBoard CreatePuttedBoard(OthelloBoard board, Point placeToPut, Side side)
        {
            var boardAtFirstNode = DeepCopier.Copy(board);
            boardAtFirstNode.PutPiece(placeToPut, side);
            return boardAtFirstNode;
        }

        private Int32 PointToScore(Point point)
        {
            var comparePoint = new ComparePoint();
            if (this.Score100PointList.Contains(point, comparePoint))
            {
                return 100;
            }
            if (this.Score20PointList.Contains(point, comparePoint))
            {
                return 20;
            }
            if (this.ScoreMinus50PointList.Contains(point, comparePoint))
            {
                return -50;
            }
            if (this.ScoreMinus10PointList.Contains(point, comparePoint))
            {
                return -10;
            }
            return 0;
        }
    }
}
