using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mime.MediaTypeNames;

namespace OthelloClassLibrary.Models
{
    public class MyOthelloModel
    {
        public static Side GetSide(Turn turn)
        {
            return turn == Turn.First ? Side.Black : Side.White;
        }

        private OthelloBoard OthelloBoard;

        public IPlayer PlayerFirst { get; private set; }
        public IPlayer PlayerSecond { get; private set; }

        public Log Log { get; } = new Log();

        public Int32 NumberOfBlackPiece
        {
            get
            {
                return this.OthelloBoard.CountOneSidePiece(Side.Black);
            }
        }
        public Int32 NumberOfWhitePiece
        {
            get
            {
                return this.OthelloBoard.CountOneSidePiece(Side.White);
            }
        }

        private Turn _Turn = Turn.First;
        public Turn Turn
        {
            get
            {
                return this._Turn;
            }
            private set
            {
                this._Turn = value;
                this.TurnChangedEvent?.Invoke(value);
            }
        }

        private GameState _GameState = GameState.SelectSide;
        public GameState GameState
        {
            get
            {
                return this._GameState;
            }
            private set
            {
                this._GameState = value;
                this.GameStateChangedEvent?.Invoke(value,this.Turn);
            }
        }

        public IPlayer CurrentPlayer
        {
            get
            {
                return this.Turn == Turn.First ? PlayerFirst : PlayerSecond;
            }
        }
        public IList<Int32> SquareNumberListCanbePut
        {
            get
            {
                return this.OthelloBoard.FindSquareNumberListCanBePut(MyOthelloModel.GetSide(this.Turn));
            }
        }
        public List<ThemeColor> ThemeColorList { get; } = new List<ThemeColor>()
        {
            ThemeColor.Default,ThemeColor.Dango,ThemeColor.Sakura,ThemeColor.Ice
        };
        public ThemeColor ThemeColor { get; set; } = ThemeColor.Default;
        public IList<OthelloPiece> Pieces
        {
            get
            {
                IList<OthelloPiece> pieces = new List<OthelloPiece>();
                foreach (var piece in this.OthelloBoard.OthelloPieceMatrix)
                {
                    pieces.Add(piece);
                }
                return pieces;
            }
        }

        public event Action TurnEndEvent;
        private event Action<GameState, Turn> GameStateChangedEvent;
        private event Action<Point, Turn> PutPieceEvent;
        private event Action<Boolean, Point, Turn> PassEvent;
        private event Action<Turn> TurnChangedEvent;

        public MyOthelloModel(Int32 boardSize, ThemeColor themeColor)
        {
            this.ThemeColor = themeColor;
            this.OthelloBoard = new OthelloBoard(boardSize);
            this.PutPieceEvent += (point, turn) => this.Log.KeepALogOfGame(turn, point);
            this.PutPieceEvent += (point, turn) => this.ProgressGame();
            this.TurnChangedEvent += (turn) => this.WaitPutPiece();
            this.PassEvent += (isPass, point, turn) => this.WaitPutPiece();


            // GameState.MatchRetiredに変わったときにthis.Log.KeepALogOfGame(false,this.Turn,-5,-5)で
            // ログを取ります。Pollingでログを受け取ったクライアント側はリタイアしたか確認するメソッドIsMatchRetired()
            // (-5,-5のpointが最後のログに書いてないか調べる)で確認でき次第GameStateをRetiredに設定します。
            this.GameStateChangedEvent += (state, turn) =>
            {
                if (state == GameState.MatchRetired)
                {
                    this.Log.KeepALogOfGame(this.Turn, new Point(-5, -5));
                }
                if (state != GameState.MatchRemaining)
                {
                    return;
                }
                this.WaitPutPiece();
            };

            this.PassEvent += (isPass, point, turn) => this.Log.KeepALogOfGame(isPass, turn, point);
        }

        public void PutPiece(Int32 squareNumber)
        {
            if (this.GameState != GameState.MatchRemaining)
            {
                return;
            }

            var point = this.OthelloBoard.SquareNumberToPoint(squareNumber);
            if (this.OthelloBoard.CanPutPiece(point, GetSide(this.Turn)) == false)
            {
                return;
            }

            this.OthelloBoard.PutPiece(point, this.Turn);
            this.PutPieceEvent?.Invoke(point, this.Turn);
        }
        public Boolean HasRightToPut()
        {
            return this.CurrentPlayer.IsAutomaton == true ? false : true;
        }

        public void SetPlayer(IPlayer player)
        {
            if (player.Turn == Turn.First)
            {
                this.PlayerFirst = player;
            }
            else
            {
                this.PlayerSecond = player;
            }
        }

        public void ChangeGameState(GameState gameState)
        {
            if (gameState == GameState.MatchRetired && this.GameState != GameState.MatchRemaining)
            {
                return;
            }
            if (gameState == GameState.MatchOver && this.GameState != GameState.MatchRemaining)
            {
                return;
            }
            this.GameState = gameState;
        }
        public void ReCreateOthelloSituation(IList<LogOfGame> previousLogOfGame, IPlayer previousPlayerFirst, IPlayer previousPlayerSecond)
        {
            if (previousLogOfGame.Count == 0)
            {
                this.SetPlayer(previousPlayerFirst);
                this.SetPlayer(previousPlayerSecond);

                this.ChangeGameState(GameState.MatchRemaining);

                return;
            }

            this.SetPlayer(new Human(Turn.First));
            this.SetPlayer(new Human(Turn.Second));
            this.ChangeGameState(GameState.MatchRemaining);
            foreach (var log in previousLogOfGame)
            {
                if (log == previousLogOfGame.Last())
                {
                    this.SetPlayer(previousPlayerFirst);
                    this.SetPlayer(previousPlayerSecond);
                }
                var squareNumber = OthelloBoard.PointToSquareNumber(log.Point);
                this.PutPiece(squareNumber);
            }
        }
        public void ReCreateOthelloSituation(IList<LogOfGame> logOfGame)
        {
            this.SetPlayer(new Human(Turn.First));
            this.SetPlayer(new Human(Turn.Second));
            this.ChangeGameState(GameState.MatchRemaining);
            foreach (var log in logOfGame)
            {
                var squareNumber = OthelloBoard.PointToSquareNumber(log.Point);
                this.PutPiece(squareNumber);
            }
        }

        public void EraceLogFromSpecifiedTurn(Int32 numberOfTurn)
        {
            this.Log.EraceLogFromSpecifiedTurn(numberOfTurn);
        }

        private void ProgressGame()
        {
            var nextTurn = this.Turn == Turn.First ? Turn.Second : Turn.First;
            var otherSide = GetSide(nextTurn);

            // 置く場所が存在する場合は継続
            if (this.OthelloBoard.HasTherePlaceToPut(otherSide) == true)
            {
                this.Turn = nextTurn;
                return;
            }

            this.PassEvent?.Invoke(true, new Point(-3, -3), nextTurn);

            if (this.OthelloBoard.HasTherePlaceToPut(GetSide(this.Turn)) == false)
            {
                this.ChangeGameState(GameState.MatchOver);
                return;
            }
        }

        private async void WaitPutPiece()
        {
            if (this.CurrentPlayer.IsAutomaton == false)
            {
                return;
            }
            if (this.OthelloBoard.HasTherePlaceToPut(GetSide(this.Turn)) == false)
            {
                return;
            }

            var point = await this.CurrentPlayer.RequestPutPiece(this.OthelloBoard);
            var squareNumber = this.OthelloBoard.PointToSquareNumber(point);
            this.PutPiece(squareNumber);
            this.TurnEndEvent?.Invoke();
        }

    }

    public enum Turn { First, Second }
    public enum ThemeColor { Default, Dango, Sakura, Ice }
    public enum GameState { SelectSide, MatchOver, MatchRetired, MatchRemaining }
}
