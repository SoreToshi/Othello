using System;
using System.Collections.Generic;
using System.Linq;

namespace OthelloClassLibrary.Models
{
    public class OthelloBoard
    {
        public OthelloPiece[,] OthelloPieceMatrix { get; set; }
        public Int32 XLength
        {
            get
            {
                return this.OthelloPieceMatrix.GetLength(0);
            }
        }

        public Int32 YLength
        {
            get
            {
                return this.OthelloPieceMatrix.GetLength(1);
            }
        }

        public OthelloBoard(Int32 boardSize)
        {
            this.OthelloPieceMatrix = new OthelloPiece[boardSize, boardSize];

            var halfSize = boardSize / 2;
            this.PutPiece(halfSize - 1, halfSize - 1, Side.White);
            this.PutPiece(halfSize, halfSize - 1, Side.Black);
            this.PutPiece(halfSize - 1, halfSize, Side.Black);
            this.PutPiece(halfSize, halfSize, Side.White);
        }

        /// <returns>ひっくり返った枚数を返します</returns>
        private (Int32, Int32) PutPiece(Int32 x, Int32 y, Side side)
        {
            var beforePutCount = this.CountOneSidePiece(side);
            this.OthelloPieceMatrix[x, y] = new OthelloPiece(side);
            this.TurnOverAPieces(new Point(x, y), side);
            var afterPutCount = this.CountOneSidePiece(side);
            return (beforePutCount, afterPutCount);
        }

        public (Int32, Int32) PutPiece(Point point, Side side)
        {
            return this.PutPiece(point.X, point.Y, side);
        }

        public (Int32, Int32) PutPiece(Point point, Turn turn)
        {
            var side = MyOthelloModel.GetSide(turn);
            return this.PutPiece(point, side);
        }

        public Int32 CountOneSidePiece(Side side)
        {
            // GetEnemyPointList()を使うためサイドを反対にする。
            side = side.ReverseSide();
            return this.FindEnemyPointList(side).Count;
        }
        public IList<Int32> FindSquareNumberListCanBePut(Side side)
        {
            return this.FindEnemyArroundEmptyPointList(side)
                .Where(point => this.CanPutPiece(point, side))
                .Select(this.PointToSquareNumber)
                .ToList();
        }

        public Boolean HasTherePlaceToPut(Side side)
        {
            return this.FindEnemyArroundEmptyPointList(side)
                .Any(point => this.CanPutPiece(point, side));
        }

        public Boolean CanPutPiece(Point point, Side side)
        {
            return this.FindArroundVectortList(point)
                .Any(vector => this.IsTurnOverLine(point, side, vector));
        }

        public Int32 PointToSquareNumber(Point point)
        {
            return point.X * XLength + point.Y;
        }

        public Point SquareNumberToPoint(Int32 squareNumber)
        {
            // 二次元配列は前の添え字が縦軸で後が横軸
            var x = squareNumber / this.XLength;
            var y = squareNumber % this.YLength;
            return new Point(x, y);
        }

        private void TurnOverAPieces(Point point, Side side)
        {
            var vectorList = this.FindArroundVectortList(point);
            foreach (var vector in vectorList.Where(vector => this.IsTurnOverLine(point, side, vector))) {
                this.TurnOverLine(point, side, vector);
            }
        }

        private Boolean IsTurnOverLine(Point point, Side side, Vector vector)
        {
            var firstRunFlag = true;
            var nextPoint = point + vector;
            // ポイントから進行方向(vector)のマスを一つずつ調べている
            while (this.IsExistsInRange(nextPoint))
            {
                if (this.IsPiecePlaced(nextPoint) == false)
                {
                    break;
                }
                if (this.IsPlayerSidePiece(nextPoint, side))
                {
                    // 自分の石の次の石が自分の石ではひっくり返せない
                    if (firstRunFlag)
                    {
                        break;
                    }
                    return true;
                }

                nextPoint = nextPoint + vector;
                firstRunFlag = false;
            }
            return false;
        }

        private void TurnOverLine(Point point, Side side, Vector vector)
        {
            var pieceForTurnOverList = this.FindPieceForTurnOverList(point, vector, side);
            foreach (var pieceForTurnOver in pieceForTurnOverList)
            {
                pieceForTurnOver.TurnOver();
            }
        }

        private IList<OthelloPiece> FindPieceForTurnOverList(Point point, Vector vector, Side side)
        {
            var pieceForTurnOverList = new List<OthelloPiece>();
            var nextPoint = point + vector;
            while (this.IsPlayerSidePiece(nextPoint, side) == false)
            {
                pieceForTurnOverList.Add(this.OthelloPieceMatrix[nextPoint.X, nextPoint.Y]);
                nextPoint = new Point(nextPoint + vector);
            }
            return pieceForTurnOverList;
        }

        private IList<Point> FindEnemyArroundEmptyPointList(Side side)
        {
            var enemyArroundPointList = new HashSet<Point>();
            var EnemyArroundEmptyPointList = new List<Point>();
            var enemyPointList = this.FindEnemyPointList(side);
            foreach (var point in enemyPointList.SelectMany(point => this.FindArroundPointList(point))) {
                enemyArroundPointList.Add(point);
            }

            foreach (var enemyArroundPoint in enemyArroundPointList.Where(p => this.IsPiecePlaced(p) == false))
            {
                EnemyArroundEmptyPointList.Add(enemyArroundPoint);
            }
            return EnemyArroundEmptyPointList;
        }

        private IList<Point> FindEnemyPointList(Side side)
        {
            // 盤面全てのマスをチェックします。
            return Enumerable
                .Range(0, XLength)
                .SelectMany(x => Enumerable.Range(0, YLength).Select(y => (x, y)))
                .Where((p) => {
                    var point = new Point(p.x, p.y);
                    return this.IsPiecePlaced(point) && this.IsEnemySidePiece(point, side);
                })
                .Select(point => new Point(point.x, point.y))
                .ToList();
        }

        private IList<Point> FindArroundPointList(Point point)
        {
            var arroundPointList = new List<Point>();
            var vectorList = this.FindArroundVectortList(point);
            foreach (var vector in vectorList)
            {
                arroundPointList.Add(point + vector);
            }
            return arroundPointList;
        }

        // for文をFindEnemyPointListを参考にして修正します。
        private IList<Vector> FindArroundVectortList(Point point)
        {
            var x = point.X;
            var y = point.Y;

            IList<Vector> aroundVectorList = new List<Vector>();
            for (var diffX = -1; diffX <= 1; diffX++)
            {
                for (var diffY = -1; diffY <= 1; diffY++)
                {
                    if (this.IsExistsInRange(new Point(x + diffX, y + diffY)))
                    {
                        aroundVectorList.Add(new Vector(diffX, diffY));
                    }
                }
            }
            // 0,0は自身を表すので除外
            aroundVectorList.Remove(new Vector(0, 0));

            return aroundVectorList;
        }
        private Boolean IsPiecePlaced(Point point)
        {
            return OthelloPieceMatrix[point.X, point.Y] != null;
        }

        private Boolean IsOutOfRange(Point point)
        {
            var x = point.X;
            var y = point.Y;
            if (this.XLength - 1 < x || this.YLength - 1 < y)
            {
                return true;
            }
            if (0 > x || 0 > y)
            {
                return true;
            }
            return false;
        }

        private Boolean IsExistsInRange(Point point)
        {
            return this.IsOutOfRange(point) == false;
        }

        private Boolean IsPieceBlack(Point point)
        {
            return this.OthelloPieceMatrix[point.X, point.Y].Side == Side.Black;
        }

        private Boolean IsPlayerSidePiece(Point point, Side side)
        {
            if (Side.Black == side && this.IsPieceBlack(point) == true)
            {
                return true;
            }
            if (Side.White == side && this.IsPieceBlack(point) == false)
            {
                return true;
            }
            return false;
        }

        private Boolean IsEnemySidePiece(Point point, Side side)
        {
            return this.IsPlayerSidePiece(point, side) == false;
        }
    }
}
