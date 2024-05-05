using OthelloClassLibrary.Models;
using Timer = System.Timers.Timer;

namespace MyOthelloWeb.Models
{
    public class AccessMonitor
    {
        public void MonitorAccessTime(Int32 roomNumber)
        {
            var room = OthelloManager.GetRoom(roomNumber);
            if (room == null)
            {
                return;
            }
            if (room.NumberOfConnection > 1)
            {
                return;
            }

            var othelloGameMode = room.Model.GameMode;

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
            var room = OthelloManager.GetRoom(roomNumber);
            if (room == null)
            {
                return;
            }
            var playerInfo = room.PlayerInfos[0];
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
            var room = OthelloManager.GetRoom(roomNumber);
            if (room == null)
            {
                return;
            }
            var playerInfos = room.PlayerInfos;
            var othello = room.Model;
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
                        playerInfo.value.InvertIsAccess();
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

        private Boolean IsPlayerDisconected(PlayerAccessInfo playerInfo, Int32 oldPlayerAccessTime)
        {
            return playerInfo.IsPlayerAccess && oldPlayerAccessTime == playerInfo.PlayerAccessTime;
        }
    }
}
