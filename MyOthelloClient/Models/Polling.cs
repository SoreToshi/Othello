using MyOthelloClient.Models;
using OthelloClassLibrary.Models;
using Timer = System.Timers.Timer;

namespace MyOthelloClient.Models
{
    public class Polling
    {
        public async void ReflectServerSituation(ClientManager clientManager, Action<IList<LogOfGame>> callback)
        {
            // 最初にサーバーのlogとクライアントのlogを合わせるための処理が入ります。
            var logOfGameList = await HitApi.FetchLogOnTheServer(clientManager.OthelloRoomNumber, clientManager.Id);
            callback(logOfGameList);

            if (this.IsGameStateSelectSide(logOfGameList))
            {
                clientManager.ChangeGameState(GameState.SelectSide);
            }

            if (this.IsRetired(logOfGameList))
            {
                clientManager.RetireProcess(logOfGameList);
            }

            var pollingTimer = new Timer(500); // msec

            pollingTimer.Elapsed += async (sender, e) =>
            {
                var isMovedToRoomSelect = clientManager.OthelloRoomNumber == 0;
                if (isMovedToRoomSelect)
                {
                    pollingTimer.Stop();
                    pollingTimer.Dispose();
                    return;
                }

                var logOfGameList = await HitApi.FetchLogOnTheServer(clientManager.OthelloRoomNumber, clientManager.Id);

                if (clientManager.GameState == GameState.SelectSide)
                {
                    return;
                }

                if (this.IsLogUpdated(clientManager.LogOfGameCount, logOfGameList) == false)
                {
                    return;
                }

                if (this.IsRetired(logOfGameList))
                {
                    clientManager.RetireProcess(logOfGameList);
                    return;
                }

                callback(logOfGameList);

                if (this.IsGameStateSelectSide(logOfGameList))
                {
                    clientManager.ChangeGameState(GameState.SelectSide);
                }
            };

            pollingTimer.Start();
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

        private Boolean IsGameStateSelectSide(IList<LogOfGame> logOfGameList)
        {
            if (logOfGameList.Count == 0)
            {
                return false;
            }
            // Restartした時サーバーのLogでPoint(-8,-8)として記録されます。
            return logOfGameList.Last().Point.X == -8;
        }


        public Task WaitOpponent(Int32 roomNumber, String identificationNumber)
        {
            var tcs = new TaskCompletionSource();
            var pollingTimer = new Timer(1000); // msec

            pollingTimer.Elapsed += async (sender, e) =>
            {
                var opponentActionString = await HitApi.FetchModeSelectOpponentAction(roomNumber, identificationNumber);
                if (opponentActionString != "SelectedSide") {
                    return;
                }

                pollingTimer.Stop();
                pollingTimer.Dispose();
                tcs.SetResult();
            };

            pollingTimer.Start();

            return tcs.Task;
        }
    }
}
