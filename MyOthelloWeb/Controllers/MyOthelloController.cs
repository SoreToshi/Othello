﻿using Microsoft.AspNetCore.Mvc;
using MyOthelloWeb.Models;
using OthelloClassLibrary.Models;

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
            var accessMonitor = new AccessMonitor();
            this.ReturnIdentificationNumberEvent += this.InvertIsAccess;
            this.InvertIsAccessEvent += accessMonitor.MonitorAccessTime;
            this.ReturnLogEvent += this.AddAccessTime;
        }

        [HttpGet]
        [Route("fetchroomsinformationforclient")]
        public String ReturnRoomsInformationForClient()
        {
            var roomsInformationLine = OthelloManager.Rooms.Select((roomInformation) => $"{roomInformation.Key}@{roomInformation.Value.Model.GameMode}@{roomInformation.Value.NumberOfConnection}");
            var roomsInformationString = String.Join(",", roomsInformationLine);
            return roomsInformationString;
        }

        [HttpGet]
        [Route("fetchnumberofconnections")]
        public String ReturnNumberOfConnections()
        {
            var numberOfConnectionLine = OthelloManager.Rooms.Select((othelloRooms) => $"{othelloRooms.Key}@{othelloRooms.Value.NumberOfConnection}");
            var numberOfConnectionString = String.Join(",", numberOfConnectionLine);
            return numberOfConnectionString;
        }

        [HttpGet]
        [Route("fetchidentificationnumber{RoomNumber}")]
        public String ReturnIdentificationNumber(Int32 roomNumber)
        {
            var room = OthelloManager.GetRoom(roomNumber);
            if (room == null)
            {
                return "NoIdentificationNumberLeft";
            }

            try
            {
                // Eventで一人目、二人目が部屋に接続したと設定するメソッドを呼び出します。
                var identificationNumber = room.FindAvailableIdentificationNumber();
                this.ReturnIdentificationNumberEvent?.Invoke(roomNumber, identificationNumber);
                return identificationNumber.ToString();
            }
            catch (InvalidCastException) 
            {
                return "NoIdentificationNumberLeft";
            }
        }

        private void InvertIsAccess(Int32 roomNumber, IdentificationNumber identificationNumber)
        {
            var playerInfo = OthelloManager.OthelloRooms[roomNumber].PlayerInfos.First(infos => infos.IdentificationNumber == identificationNumber);
            
            playerInfo.InvertIsAccess();

            // 接続した際に、その後接続を継続しているか監視するメソッドをイベントで呼び出します。
            this.InvertIsAccessEvent?.Invoke(roomNumber);
        }

        [HttpGet]
        [Route("fetchlog{RoomNumber}&{IdentificationNumber}")]
        public String ReturnLog(Int32 roomNumber, IdentificationNumber identificationNumber)
        {
            var othello = OthelloManager.OthelloRooms[roomNumber].Model;
            var logForReturnLog = new Log(othello);
            var logString = LogSerializer.Serialize(logForReturnLog.LogOfGame);

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

            var listOfLog = LogSerializer.Deserialize(logString);
            othello.ReCreateOthelloSituation(listOfLog);

            othello.ChangeGameState(GameState.SelectSide);
        }
    }
}