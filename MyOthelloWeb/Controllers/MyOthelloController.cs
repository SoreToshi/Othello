using Microsoft.AspNetCore.Mvc;
using OthelloClassLibrary.Models;
using Timer = System.Timers.Timer;

namespace MyOthelloWeb.Controllers
{
    [ApiController]
    [Route("/api")]
    public class MyOthelloController : ControllerBase
    {

        private event Action<Int32, IdentificationNumber> ReturnIdentificationNumberEvent;
        private event Action<Int32> InvertIsAccessEvent;
        private event Action<Int32, IdentificationNumber> ReturnLogEvent;
        public MyOthelloController()
        {
            this.ReturnIdentificationNumberEvent += this.InvertIsAccess;
            this.InvertIsAccessEvent += this.MonitorAccessTime;
            this.ReturnLogEvent += this.AddAccessTime;
        }

        [HttpGet]
        [Route("fetchroomsinformationforclient")]
        public String ReturnRoomsInformationForClient()
        {
            var roomsInformationLine = OthelloManager.OthelloRooms.Select((roomInformation) => $"{roomInformation.Key}@{roomInformation.Value.Model.GameMode}@{roomInformation.Value.NumberOfConnection}");
            var roomsInformationString = String.Join(",", roomsInformationLine);
            return roomsInformationString;
        }

        [HttpGet]
        [Route("fetchnumberofconnections")]
        public String ReturnNumberOfConnections()
        {
            var numberOfConnectionLine = OthelloManager.OthelloRooms.Select((othelloRooms) => $"{othelloRooms.Key}@{othelloRooms.Value.NumberOfConnection}");
            var numberOfConnectionString = String.Join(",", numberOfConnectionLine);
            return numberOfConnectionString;
        }

        [HttpGet]
        [Route("fetchidentificationnumber{RoomNumber}")]
        public String ReturnIdentificationNumber(Int32 roomNumber)
        {
            // Eventで一人目、二人目が部屋に接続したと設定するメソッドを呼び出します。
            foreach (var playerInfo in OthelloManager.OthelloRooms[roomNumber].PlayerInfos)
            {
                if (playerInfo.IsPlayerAccess == false)
                {
                    this.ReturnIdentificationNumberEvent?.Invoke(roomNumber, playerInfo.IdentificationNumber);
                    return playerInfo.IdentificationNumber.ToString();
                }
            }
            return "NoIdentificationNumberLeft";
        }

        private void InvertIsAccess(Int32 roomNumber, IdentificationNumber identificationNumber)
        {
            var playerInfo = OthelloManager.OthelloRooms[roomNumber].PlayerInfos.First(infos => infos.IdentificationNumber == identificationNumber);
            
            playerInfo.InvertIsAccess();

            // 接続した際に、その後接続を継続しているか監視するメソッドをイベントで呼び出します。
            this.InvertIsAccessEvent.Invoke(roomNumber);
        }

        private void MonitorAccessTime(Int32 roomNumber)
        {
            if (OthelloManager.OthelloRooms[roomNumber].NumberOfConnection != 1)
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

        [HttpGet]
        [Route("fetchlog{RoomNumber}&{IdentificationNumber}")]
        public String ReturnLog(Int32 roomNumber, IdentificationNumber identificationNumber)
        {
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            var logForReturnLog = new Log();
            if (othello.Log.LogOfGame.Count != 0)
            {
                foreach (var log in othello.Log.LogOfGame)
                {
                    logForReturnLog.KeepALogOfGame(log.IsPass, log.Turn, log.Point);
                }
            }

            // セレクトサイド状態の時に-8,-8を入れることで受け取ったクライアントがSelectSide状態であることを反映できます。
            if (othello.GameState == GameState.SelectSide)
            {
                logForReturnLog.KeepALogOfGame(othello.Turn, new Point(-8, -8));
            }

            // マッチリタイア状態の時にLogのPointにxに-5を入れることで受け取ったクライアントがリタイア状態であることを、yに-1,-2を入れることで
            // 先手と後手どちらがリタイアしたかを反映できます。
            if (othello.GameState == GameState.MatchRetired)
            {
                var retiredTurnNumber = othello.RetiredTurn == Turn.First ? -1 : -2;
                logForReturnLog.KeepALogOfGame(othello.Turn, new Point(-5, retiredTurnNumber));
            }

            var logLines = logForReturnLog.LogOfGame.Select((log) => $"{log.IsPass}@{log.Turn}@{log.Point.X}{log.Point.Y}");
            var logString = String.Join(",", logLines);

            this.ReturnLogEvent(roomNumber, identificationNumber);

            return logString;
        }
        private void AddAccessTime(Int32 roomNumber, IdentificationNumber identificationNumber)
        {
            var playerInfo = OthelloManager.OthelloRooms[roomNumber].PlayerInfos.First(
                (info) => info.IdentificationNumber == identificationNumber);

            playerInfo.AddAccessTime();
        }

        [HttpGet]
        [Route("start{RoomNumber}&{Turn}&{IdentificationNumber}")]
        public void Start(Int32 roomNumber, Turn turn, IdentificationNumber identificationNumber)
        {
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            var player = turn == Turn.First ? new Human(Turn.First) : new Human(Turn.Second);
            // Cpu戦の処理
            if (othello.GameMode == GameMode.VsCpu)
            {
                this.StartVsCpuProgress(roomNumber, player, identificationNumber);
            }
            // 対人戦の処理
            else
            {
                this.StartVsHumanProgress(roomNumber, player, identificationNumber);
            }
        }
        private void StartVsCpuProgress(Int32 roomNumber, IPlayer player, IdentificationNumber identificationNumber)
        {
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            var playerInfo = OthelloManager.OthelloRooms[roomNumber].PlayerInfos.First((info) => info.IdentificationNumber == identificationNumber);
            playerInfo.Turn = player.Turn;
            othello.SetPlayer(player);
            var playerCpu = new Cpu(player.Turn == Turn.First ? Turn.Second : Turn.First);
            othello.SetPlayer(playerCpu);

            othello.ChangeGameState(GameState.MatchRemaining);
        }

        private void StartVsHumanProgress(Int32 roomNumber, IPlayer player, IdentificationNumber identificationNumber)
        {
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            var playerInfo = OthelloManager.OthelloRooms[roomNumber].PlayerInfos.First((info) => (info.IdentificationNumber == identificationNumber));
            // 選択した先攻後攻が選ばれていた場合の処理
            if (this.IsSideSelected(roomNumber, player.Turn))
            {
                return;
            }

            othello.SetPlayer(player);
            playerInfo.InvertIsTurnSelected();
            playerInfo.Turn = player.Turn;

            if (IsOpponentSelected(roomNumber, player.Turn , identificationNumber) == false)
            {
                return;
            }

            othello.ChangeGameState(GameState.MatchRemaining);
        }
        private Boolean IsSideSelected(Int32 roomNumber, Turn turn)
        {
            var playerInfos = OthelloManager.OthelloRooms[roomNumber].PlayerInfos.ToList();
            return playerInfos.Exists(info => info.IsTurnSelected && info.Turn == turn);
        }
        private Boolean IsOpponentSelected(Int32 roomNumber, Turn turn, IdentificationNumber identificationNumber)
        {

            var playerInfos = OthelloManager.OthelloRooms[roomNumber].PlayerInfos;
            var opponentInfos = playerInfos.Where((info) => (info.IdentificationNumber != identificationNumber));

            return opponentInfos.All(info => info.IsTurnSelected);
        }

        [HttpGet]
        [Route("fetchplayerstatus{RoomNumber}&{Turn}&{IdentificationNumber}")]
        public String ReturnPlayerStatus(Int32 roomNumber, Turn turn, IdentificationNumber identificationNumber)
        {

            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            var playerTurn = turn;

            if (othello.GameMode == GameMode.VsCpu)
            {
                return "Start";
            }

            if (othello.GameMode == GameMode.VsHuman)
            {
                return FindVsHumanPlayerStatus(roomNumber, playerTurn, identificationNumber);
            }

            return "GameModeDoesNotExist";
        }

        private String FindVsHumanPlayerStatus(Int32 roomNumber, Turn playerTurn, IdentificationNumber identificationNumber)
        {
            var playerInfo = OthelloManager.OthelloRooms[roomNumber].PlayerInfos.First((info) => (info.IdentificationNumber == identificationNumber));
            // 選ばれていた場合の処理
            if (this.IsSideSelected(roomNumber, playerTurn))
            {
                return "AlreadySelected";
            }

            // 相手が選択しているかどうかをplayerTurnを反対にして調べます
            var opponentTurn = playerTurn == Turn.First ? Turn.Second : Turn.First;
            if (IsSideSelected(roomNumber, opponentTurn))
            {
                return "Start";
            }
            else
            {
                return "WaitOpponent";
            }
        }


        [HttpGet]
        [Route("fetchopponentaction{RoomNumber}&{IdentificationNumber}")]
        public String ReturnOpponentAction(Int32 roomNumber, IdentificationNumber identificationNumber)
        {

            var playerInfos = OthelloManager.OthelloRooms[roomNumber].PlayerInfos;
            var opponentInfos = playerInfos.Where((info) => (info.IdentificationNumber != identificationNumber));

            return opponentInfos.All((info) => (info.IsTurnSelected)) ? "SelectedSide" : "DoNothing";
        }

        [Route("putpiece{RoomNumber}")]
        [HttpPost]
        public void PutPiece(Int32 roomNumber, [FromBody] Int32 squareNumber)
        {
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            if (othello.HasRightToPut() == false)
            {
                return;
            }
            othello.PutPiece(squareNumber);
        }

        [HttpGet]
        [Route("restart{RoomNumber}")]
        public void RestartOthello(Int32 roomNumber)
        {
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            var gameMode = othello.GameMode;
            var playerInfos = OthelloManager.OthelloRooms[roomNumber].PlayerInfos;
            foreach (var playerInfo in playerInfos)
            {
                if (playerInfo.IsTurnSelected)
                {
                    playerInfo.InvertIsTurnSelected();
                }
            }
            OthelloManager.OthelloRooms[roomNumber].RecreateOthello(gameMode);
        }

        [HttpGet]
        [Route("retire{RoomNumber}&{RetireSideTurn}")]
        public void RetireMatch(Int32 roomNumber, Turn retireSideTurn)
        {
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            othello.RetiredTurn = retireSideTurn;
            othello.ChangeGameState(GameState.MatchRetired);
        }

        [HttpPost]
        [Route("backfromlog{RoomNumber}")]
        public void BackFromLog(Int32 roomNumber, [FromBody] Int32 numberOfLog)
        {
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            othello.EraceLogFromSpecifiedTurn(numberOfLog);

            var previousLogOfGame = othello.Log.LogOfGame;
            var previousPlayerFirst = othello.PlayerFirst;
            var previousPlayerSecond = othello.PlayerSecond;

            this.RestartOthello(roomNumber);

            // RestartOthelloでnewされるため新しい変数に代入しなおします。
            var newOthello = OthelloManager.OthelloRooms[roomNumber].Model;
            newOthello.ReCreateOthelloSituation(previousLogOfGame, previousPlayerFirst, previousPlayerSecond);
        }

        [HttpPost]
        [Route("loadfile{RoomNumber}")]
        public void LoadLogFile(Int32 roomNumber, [FromBody] String logString)
        {
            this.RestartOthello(roomNumber);

            var othello = OthelloManager.OthelloRooms[roomNumber].Model;

            var listOfLog = LoadFiles(logString);
            othello.ReCreateOthelloSituation(listOfLog);

            othello.ChangeGameState(GameState.SelectSide);
        }

        private IList<LogOfGame> LoadFiles(String log)
        {
            var listOfLog = new List<LogOfGame>();
            var logInfoArr = log.Split(',');
            foreach (var line in logInfoArr)
            {
                var splitLine = line.Split('@');
                var isPass = splitLine[0] == "True" ? true : false;
                var turn = splitLine[1] == "First" ? Turn.First : Turn.Second;
                String x;
                String y;
                if (isPass == true)
                {
                    x = splitLine[2].Substring(0, 2);
                    y = splitLine[2].Substring(2, 2);
                }
                else
                {
                    x = splitLine[2][0].ToString();
                    y = splitLine[2][1].ToString();
                }
                var point = new Point(Int32.Parse(x), Int32.Parse(y));
                listOfLog.Add(new LogOfGame(isPass, turn, point));
            }
            return listOfLog;
        }
    }
}