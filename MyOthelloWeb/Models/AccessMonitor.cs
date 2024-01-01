using OthelloClassLibrary.Models;
using Timer = System.Timers.Timer;

namespace MyOthelloWeb.Models
{
    public class AccessMonitor
    {
        public void MonitorAccessTime(Int32 roomNumber)
        {
            if (OthelloManager.OthelloRooms[roomNumber].NumberOfConnection > 1)
            {
                return;
            }

            var othelloGameMode = OthelloManager.OthelloRooms[roomNumber].Model.GameMode;

            if (othelloGameMode == GameMode.VsCpu)
            {
                this.MonitorVsCpu(roomNumber);
            }

            if (othelloGameMode == GameMode.VsHuman)
            {
                this.MonitorVsHuman(roomNumber);
            }
        }
        private void MonitorVsCpu(Int32 roomNumber)
        {
            // client側のpollingTimerよりこのタイマーは遅くします。
            var monitorAccessTimer = new Timer(4000); // msec
            var playerInfo = OthelloManager.OthelloRooms[roomNumber].PlayerInfos[0];
            var oldPlayerAccessTime = 0;

            monitorAccessTimer.Elapsed += (sender, e) =>
            {
                if (oldPlayerAccessTime == playerInfo.PlayerAccessTime)
                {
                    monitorAccessTimer.Stop();
                    monitorAccessTimer.Dispose();

                    // // プレイヤーがいなくなった際にルームを初期化します。
                    OthelloManager.RecreateRoomInformationForServer(roomNumber);
                }
                oldPlayerAccessTime = playerInfo.PlayerAccessTime;
            };

            monitorAccessTimer.Start();
        }
        private void MonitorVsHuman(Int32 roomNumber)
        {
            // client側のpollingTimerよりこのタイマーは遅くします。
            var monitorAccessTimer = new Timer(4000); // msec
            var playerInfos = OthelloManager.OthelloRooms[roomNumber].PlayerInfos;
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            var oldPlayerAccessTimeList = new List<Int32>();
            foreach (var playerInfo in playerInfos)
            {
                oldPlayerAccessTimeList.Add(0);
            }

            monitorAccessTimer.Elapsed += (sender, e) =>
            {
                foreach (var playerInfo in playerInfos.Select((value, index) => new { value, index }))
                {
                    if (this.IsPlayerDisconected(playerInfo.value, oldPlayerAccessTimeList[playerInfo.index]))
                    {
                        this.DisconectedProgress(playerInfo.value, othello.GameState);
                    }
                }
                if (playerInfos.All(info => !info.IsPlayerAccess))
                {
                    monitorAccessTimer.Stop();
                    monitorAccessTimer.Dispose();

                    // プレイヤーがいなくなった際にルームを初期化します。
                    OthelloManager.RecreateRoomInformationForServer(roomNumber);
                }

                foreach (var playerInfo in playerInfos.Select((value, index) => new { value, index }))
                {
                    oldPlayerAccessTimeList[playerInfo.index] = playerInfo.value.PlayerAccessTime;
                }
            };

            monitorAccessTimer.Start();
        }
        private void DisconectedProgress(PlayerAccessInfo playerInfo, GameState gameState)
        {
            playerInfo.InvertIsAccess();

            if (!playerInfo.IsTurnSelected)
            {
                return;
            }

            // リタイア中新しく入ってきた人のターンを選べるようにすると、
            // 対戦相手がまだリタイア画面にもかかわらずゲームが始まってしまうのでリターンします。
            if (gameState == GameState.MatchRetired)
            {
                return;
            }
            playerInfo.InvertIsTurnSelected();
        }

        private Boolean IsPlayerDisconected(PlayerAccessInfo playerInfo, Int32 oldPlayerAccessTime)
        {
            return playerInfo.IsPlayerAccess && oldPlayerAccessTime == playerInfo.PlayerAccessTime;
        }
    }
}
