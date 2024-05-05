using OthelloClassLibrary.Models;
using Timer = System.Timers.Timer;

namespace MyOthelloClient.Models
{
    public class Polling
    {
        private MyOthelloModel Othello
        {
            get
            {
                return ClientManager.Model;
            }
        }

        public async void PollingToReflectServerSituation(Int32 roomNumber, String iD)
        {
            // 最初にサーバーのlogとクライアントのlogを合わせるための処理が入ります。
            var logOfGameList = await HitApi.FetchLogOnTheServer(roomNumber, iD);
            this.BuildOthelloFromLog(logOfGameList);

            if (this.IsGameStateSelectSide(logOfGameList))
            {
                this.Othello.ChangeGameState(GameState.SelectSide);
            }

            if (this.IsRetired(logOfGameList))
            {
                this.RetireProcess(logOfGameList);
            }

            var pollingTimer = new Timer(500); // msec

            pollingTimer.Elapsed += async (sender, e) =>
            {
                if (IsMovedToRoomSelect())
                {
                    pollingTimer.Stop();
                    pollingTimer.Dispose();

                    return;
                }

                var logOfGameList = await HitApi.FetchLogOnTheServer(roomNumber, iD);

                if (this.Othello.GameState == GameState.SelectSide)
                {
                    return;
                }
                if (this.IsLogUpdated(this.Othello.Log.LogOfGame.Count(), logOfGameList) == false)
                {
                    return;
                }

                if (this.IsRetired(logOfGameList))
                {
                    this.RetireProcess(logOfGameList);
                    return;
                }

                this.BuildOthelloFromLog(logOfGameList);

                if (this.IsGameStateSelectSide(logOfGameList))
                {
                    
                    this.Othello.ChangeGameState(GameState.SelectSide);
                }
            };

            pollingTimer.Start();
        }

        private void BuildOthelloFromLog(IList<LogOfGame> logOfGameList)
        {
            ClientManager.RecreateOthelloModel();

            this.Othello.ReCreateOthelloSituation(logOfGameList);
        }
        private Boolean IsMovedToRoomSelect()
        {
            return ClientManager.OthelloRoomNumber == 0;
        }
        private Boolean IsLogUpdated(Int32 logCount, IList<LogOfGame> logOfGame)
        {
            if (logOfGame.Count() != 0)
            {
                // セレクトサイドの場合はlogの数に関係なくtrueを返します。
                if (logOfGame.Last().Point.X == -8)
                {
                    return true;
                }
            }
            return logCount != logOfGame.Count();
        }
        private Boolean IsRetired(IList<LogOfGame> logOfGameList)
        {
            if (logOfGameList.Count == 0)
            {
                return false;
            }
            // Retireした時サーバーのログでPoint.X = -5と記録されています。
            return logOfGameList.Last().Point.X == -5;
        }
        private void RetireProcess(IList<LogOfGame> logOfGameList)
        {
            this.Othello.RetiredTurn = logOfGameList.Last().Point.Y == -1 ? Turn.First : Turn.Second;
            this.Othello.ChangeGameState(GameState.MatchRetired);
        }
        private Boolean IsGameStateSelectSide(IList<LogOfGame> logOfGameList)
        {
            if (logOfGameList.Count == 0)
            {
                return false;
            }
            // Restartした時サーバーのLogでPoint(-8,-8)として記録されます。
            return logOfGameList.Last().Point.X == -8;
        }


        public void PollingToWaitOpponent(Int32 roomNumber, String identificationNumber)
        {
            var pollingTimer = new Timer(1000); // msec

            pollingTimer.Elapsed += async (sender, e) =>
            {
                // 待機中にルームセレクトに戻った場合クライアントのRoomNumberは0になります。。
                if (roomNumber == 0)
                {
                    pollingTimer.Stop();
                    pollingTimer.Dispose();

                    return;
                }

                var opponentActionString = await HitApi.FetchModeSelectOpponentAction(roomNumber, identificationNumber);
                if (opponentActionString == "DoNothing")
                {
                    return;
                }
                if (opponentActionString == "SelectedSide")
                {
                    pollingTimer.Stop();
                    pollingTimer.Dispose();

                    ClientManager.PlayerStatusInSelect = PlayerStatusInSelect.Nothing;
                    this.Othello.ChangeGameState(GameState.MatchRemaining);
                    return;
                }
            };

            pollingTimer.Start();
        }
    }
}
