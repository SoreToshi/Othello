using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OthelloClassLibrary.Models;
using System.Text;
using System.Text.Json;
using Timer = System.Timers.Timer;

namespace MyOthelloClient.Pages
{
    public partial class OthelloPage
    {
        private MyOthelloModel Othello;
        private Int32 OthelloRoomNumber { get; set; }

        // ルーム入室時にサーバーから割り振られます。
        private IdentificationNumber IdentificationNumber { get; set; }

        private Dictionary<Int32, RoomInformationForClient> OthelloRooms = new Dictionary<Int32, RoomInformationForClient> {
            {0, new RoomInformationForClient(GameMode.VsHuman, 0)}
        };

        private PlayerStatusInSelect _ModeSelectPlayerStatus = PlayerStatusInSelect.Nothing;
        private PlayerStatusInSelect ModeSelectPlayerStatus
        {
            get
            {
                return this._ModeSelectPlayerStatus;
            }
            set
            {
                this._ModeSelectPlayerStatus = value;
                this.ModeSelectPlayerStatusChangedEvent?.Invoke(value);
            }
        }
        private Turn MyTurn;
        private IDictionary<Int32, Int32> RoomCapacity
        {
            get
            {
                return this.OthelloRooms.ToDictionary(othelloRooms => othelloRooms.Key, othelloRooms => othelloRooms.Value.GameModeOfRoom == GameMode.VsHuman ? 2 : 1);
            }
        }
        private Int32 NumberOfBlackPiece
        {
            get
            {
                return this.Othello.NumberOfBlackPiece;
            }
        }
        private Int32 NumberOfWhitePiece
        {
            get
            {
                return this.Othello.NumberOfWhitePiece;
            }
        }
        private IList<LogOfGame> LogOfGame
        {
            get
            {
                return this.Othello.Log.LogOfGame;
            }
        }
        private GameState GameState
        {
            get
            {
                return this.Othello.GameState;
            }
        }
        private IList<Int32> SquareNumberListCanBePut
        {
            get
            {
                return this.Othello.SquareNumberListCanbePut;
            }
        }
        private IList<ThemeColor> ThemeColorList
        {
            get
            {
                return this.Othello.ThemeColorList;
            }
        }
        private IEnumerable<Tuple<OthelloPiece, Int32>> Pieces
        {
            get
            {
                return this.Othello.Pieces.Select((piece, i) => Tuple.Create(piece, i));
            }
        }


        private String OthelloTheme = "default";

        private event Action UpdateRoomsInformationEvent;
        private event Action<Int32> SelectOthelloRoomEvent;
        private event Action<Int32> SetNumberOfConnectionEvent;
        private event Action OnLoadLogFileEvent;
        private event Action<PlayerStatusInSelect> ModeSelectPlayerStatusChangedEvent;
        private event Action MoveToRoomSelectEvent;

        public OthelloPage()
        {
            this.Othello = new MyOthelloModel(OthelloManager.BoardSize, ThemeColor.Default);
            this.Othello.GameStateChangedEvent += (state, turn) => this.StateHasChanged();
            this.Othello.TurnEndEvent += this.StateHasChanged;
            this.UpdateRoomsInformationEvent += this.StateHasChanged;
            this.SelectOthelloRoomEvent += (roomNumber) => this.PollingToReflectServerSituation();
            this.SetNumberOfConnectionEvent += (numberOfRoom) => this.StateHasChanged();
            this.OnLoadLogFileEvent += this.StateHasChanged;
            this.ModeSelectPlayerStatusChangedEvent += (playerStatusInSelect) => this.StateHasChanged();
            this.MoveToRoomSelectEvent += () => this.ModeSelectPlayerStatus = PlayerStatusInSelect.Nothing;
            this.MoveToRoomSelectEvent += this.UpdateNumberOfConnections;
            this.UpdateRoomsInformation();
        }

        private async void UpdateRoomsInformation()
        {
            var roomsInformationString = await this.FetchRoomsInformationForClient();
            var roomsInformation = this.ParseRoomsInformationStringToDictionary(roomsInformationString);
            this.OthelloRooms = roomsInformation;

            this.UpdateRoomsInformationEvent?.Invoke();
        }
        private async Task<String> FetchRoomsInformationForClient()
        {
            var fetchRoomUrl = "https://localhost:7146/api/fetchroomsinformationforclient";
            var result = await MyHttpClient.GetAsync(fetchRoomUrl);
            return await result.Content.ReadAsStringAsync();
        }
        private Dictionary<Int32, RoomInformationForClient> ParseRoomsInformationStringToDictionary(String roomsInformationString)
        {
            var roomsInfoArr = roomsInformationString.Split(',');
            var roomsInformationDic = new Dictionary<Int32, RoomInformationForClient>();
            foreach (var roomInfo in roomsInfoArr)
            {
                var roomInfoLine = roomInfo.Split('@');
                var roomNumber = Int32.Parse(roomInfoLine[0]);
                var gameMode = roomInfoLine[1] == "VsHuman" ? GameMode.VsHuman : GameMode.VsCpu;
                var numberOfConnections = Int32.Parse(roomInfoLine[2]);
                roomsInformationDic.Add(roomNumber, new RoomInformationForClient(gameMode, numberOfConnections));
            }

            return roomsInformationDic;
        }

        private async void UpdateNumberOfConnections()
        {
            this.SetNumberOfConnections(await this.FetchNumberOfConnections());
        }

        private async Task<String> FetchNumberOfConnections()
        {
            var fetchNumberOfConnectionsUrl = "https://localhost:7146/api/fetchnumberofconnections";
            var result = await MyHttpClient.GetAsync(fetchNumberOfConnectionsUrl);
            return await result.Content.ReadAsStringAsync();
        }
        private void SetNumberOfConnections(String numberOfConnectionOfRoomsString)
        {
            var numberOfConnectionOfRoomsArray = numberOfConnectionOfRoomsString.Split(',');
            foreach (var numberOfConnection in numberOfConnectionOfRoomsArray)
            {
                var numberOfConnectionLine = numberOfConnection.Split('@');
                var roomNumber = Int32.Parse(numberOfConnectionLine[0]);
                this.OthelloRooms[roomNumber].NumberOfConnections = Int32.Parse(numberOfConnectionLine[1]);
            }

            this.SetNumberOfConnectionEvent?.Invoke(this.OthelloRoomNumber);
        }

        private async void SelectOthelloRoom(Int32 roomNumber)
        {
            if (roomNumber <= 0 || roomNumber > this.OthelloRooms.Count)
            {
                return;
            }

            var roomsInformationString = await this.FetchRoomsInformationForClient();
            var roomsInformation = this.ParseRoomsInformationStringToDictionary(roomsInformationString);
            var roomInformation = roomsInformation[roomNumber];
            if (this.IsSelectRoomFull(roomInformation))
            {
                return;
            }

            this.IdentificationNumber = await this.FetchIdentificationNumber(roomNumber);
            this.OthelloRoomNumber = roomNumber;

            // サーバー上のログを反映するためのメソッドをEventで起こします。
            this.SelectOthelloRoomEvent?.Invoke(this.OthelloRoomNumber);
        }
        private Boolean IsSelectRoomFull(RoomInformationForClient roomInformation)
        {
            // VsHumanの部屋最大人数は2、VsCpuは1です。
            return roomInformation.GameModeOfRoom == GameMode.VsHuman
                ? roomInformation.NumberOfConnections >= 2
                : roomInformation.NumberOfConnections >= 1;
        }
        private async Task<IdentificationNumber> FetchIdentificationNumber(Int32 roomNumber)
        {
            var fetchIdentificationNumberUrl = $"https://localhost:7146/api/fetchidentificationnumber{roomNumber}";
            var result = await MyHttpClient.GetAsync(fetchIdentificationNumberUrl);

            return await result.Content.ReadAsStringAsync() == "One" ? IdentificationNumber.One : IdentificationNumber.Two;
        }

        private async void PollingToReflectServerSituation()
        {
            // 最初にサーバーのlogとクライアントのlogを合わせるための処理が入ります。
            var logOfGameList = await FetchLogOnTheServer();
            this.BuildOthelloFromServerLog(logOfGameList);
            this.Othello.ChangeGameState(GameState.SelectSide);

            var pollingTimer = new Timer(500); // msec

            pollingTimer.Elapsed += async (sender, e) =>
            {
                if (IsMovedToRoomSelect())
                {
                    pollingTimer.Stop();
                    pollingTimer.Dispose();

                    return;
                }

                var logOfGameList = await this.FetchLogOnTheServer();

                if (this.GameState == GameState.SelectSide)
                {
                    return;
                }
                if (this.IsLogUpdated(logOfGameList) == false)
                {
                    return;
                }

                if (this.IsRetired(logOfGameList))
                {
                    this.RetireProcess(logOfGameList);
                    return;
                }

                this.BuildOthelloFromServerLog(logOfGameList);

                if (this.IsGameStateSelectSide(logOfGameList))
                {
                    this.Othello.ChangeGameState(GameState.SelectSide);
                }
            };

            pollingTimer.Start();
        }

        private async Task<IList<LogOfGame>> FetchLogOnTheServer()
        {
            var result = await MyHttpClient.GetAsync($"https://localhost:7146/api/fetchlog{this.OthelloRoomNumber}&{this.IdentificationNumber}");

            return this.ParseLogStringToLogList(await result.Content.ReadAsStringAsync());
        }
        private IList<LogOfGame> ParseLogStringToLogList(String logString)
        {
            var logArr = logString.Split(',');
            var logOfGame = new List<LogOfGame>();

            if (this.IsLogExist(logString) == false)
            {
                return logOfGame;
            }

            return logArr.Select(OthelloClassLibrary.Models.LogOfGame.Parse).ToList();
        }
        private Boolean IsLogExist(String logString)
        {
            var logInfos = logString.Split(',');
            var isPass = logInfos[0].Split('@')[0];
            return isPass == "False" || isPass == "True";
        }

        private void BuildOthelloFromServerLog(IList<LogOfGame> logOfGameList)
        {
            this.RestartOthello();

            this.Othello.ReCreateOthelloSituation(logOfGameList);

            this.OnLoadLogFileEvent?.Invoke();
        }

        private void RestartOthello()
        {
            this.Othello = new MyOthelloModel(OthelloManager.BoardSize, this.Othello.ThemeColor);
            this.Othello.GameStateChangedEvent += (state, turn) => this.StateHasChanged();
            this.Othello.TurnEndEvent += this.StateHasChanged;
        }

        private Boolean IsMovedToRoomSelect()
        {
            // MoveToRoomSelect()でルームセレクト画面に戻った際
            // クライアントのRoomNumberは存在しない部屋番0に設定されます。
            return this.OthelloRoomNumber == 0;
        }

        private Boolean IsLogUpdated(IList<LogOfGame> logOfGame)
        {
            if (logOfGame.Count() != 0)
            {
                // セレクトサイドの場合はlogの数に関係なくtrueを返します。
                if (logOfGame.Last().Point.X == -8)
                {
                    return true;
                }
            }
            return this.Othello.Log.LogOfGame.Count() != logOfGame.Count();
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

        private async void StartGame(IPlayer player)
        {
            if (this.ModeSelectPlayerStatus == PlayerStatusInSelect.Waiting)
            {
                return;
            }
            BuildModeSelectPlayerSituation(player);

            var StartUrl = $"https://localhost:7146/api/start{this.OthelloRoomNumber}&{player.Turn}&{this.IdentificationNumber}";
            await MyHttpClient.GetAsync(StartUrl);
        }
        private async void BuildModeSelectPlayerSituation(IPlayer player)
        {
            var fetchPlayerStatusUrl = $"https://localhost:7146/api/fetchplayerstatus{this.OthelloRoomNumber}&{player.Turn}&{this.IdentificationNumber}";
            var result = await MyHttpClient.GetAsync(fetchPlayerStatusUrl);
            var playerStatusString = await result.Content.ReadAsStringAsync();

            if (IsPlayerStatusStringCorrect(playerStatusString) == false)
            {
                return;
            }

            switch (playerStatusString)
            {
                case "WaitOpponent":
                    this.ModeSelectPlayerStatus = PlayerStatusInSelect.Waiting;
                    this.MyTurn = player.Turn;
                    this.PollingToWaitOpponent();
                    break;
                case "AlreadySelected":
                    this.ModeSelectPlayerStatus = PlayerStatusInSelect.CantSelect;
                    break;
                case "Start":
                    this.ModeSelectPlayerStatus = PlayerStatusInSelect.Nothing;
                    this.MyTurn = player.Turn;
                    this.Othello.ChangeGameState(GameState.MatchRemaining);
                    this.StateHasChanged();
                    break;

                default: return;
            }
        }
        private Boolean IsPlayerStatusStringCorrect(String playerStatusString)
        {
            return playerStatusString == "WaitOpponent"
                || playerStatusString == "AlreadySelected"
                || playerStatusString == "Start";
        }

        private void PollingToWaitOpponent()
        {
            var pollingTimer = new Timer(1000); // msec

            pollingTimer.Elapsed += async (sender, e) =>
            {
                // 待機中にルームセレクトに戻った場合クライアントのRoomNumberは0になります。。
                if (this.OthelloRoomNumber == 0)
                {
                    pollingTimer.Stop();
                    pollingTimer.Dispose();

                    return;
                }

                var opponentActionString = await this.FetchModeSelectOpponentAction(this.MyTurn);
                if (opponentActionString == "DoNothing")
                {
                    return;
                }
                if (opponentActionString == "SelectedSide")
                {
                    pollingTimer.Stop();
                    pollingTimer.Dispose();

                    this.Othello.ChangeGameState(GameState.MatchRemaining);
                    return;
                }
            };

            pollingTimer.Start();
        }
        private async Task<String> FetchModeSelectOpponentAction(Turn myTurn)
        {
            var fetchOpponentUrl = $"https://localhost:7146/api/fetchopponentaction{this.OthelloRoomNumber}&{this.IdentificationNumber}";
            var result = await MyHttpClient.GetAsync(fetchOpponentUrl);
            return await result.Content.ReadAsStringAsync();
        }


        private async void PutPiece(Int32 squareNumber)
        {
            if (this.GameState != GameState.MatchRemaining)
            {
                return;
            }
            if (this.MyTurn != Othello.Turn)
            {
                return;
            }
            var putPieceUrl = $"https://localhost:7146/api/putpiece{this.OthelloRoomNumber}";
            var jsonString = JsonSerializer.Serialize(squareNumber);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            await MyHttpClient.PostAsync(putPieceUrl, content);
        }

        private async void RestartServerOthello()
        {
            await MyHttpClient.GetAsync($"https://localhost:7146/api/restart{this.OthelloRoomNumber}");
        }

        private async void RetireMatch()
        {
            if (this.GameState != GameState.MatchRemaining)
            {
                return;
            }
            await MyHttpClient.GetAsync($"https://localhost:7146/api/retire{this.OthelloRoomNumber}&{this.MyTurn}");
        }

        private async void BackFromLog(Int32 numberOfLog)
        {
            string url = $"https://localhost:7146/api/backfromlog{this.OthelloRoomNumber}";
            var jsonString = JsonSerializer.Serialize(numberOfLog);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            await MyHttpClient.PostAsync(url, content);
        }

        private void MoveToRoomSelect()
        {
            this.RestartOthello();

            // ルームセレクトの時自身のオセロルームナンバーを初期値の0に戻します。
            this.OthelloRoomNumber = 0;

            this.MoveToRoomSelectEvent?.Invoke();
        }


        private void ChangeTheme(ThemeColor color)
        {
            var stringOfColor = this.OthelloThemeToString(color);
            this.OthelloTheme = stringOfColor;
        }
        private String OthelloThemeToString(ThemeColor themeColor)
        {
            switch (themeColor)
            {
                case ThemeColor.Default:
                    return "default";
                case ThemeColor.Dango:
                    return "dango";
                case ThemeColor.Sakura:
                    return "sakura";
                case ThemeColor.Ice:
                    return "ice";
                default:
                    return "default";
            }
        }


        private async void UpLoadLogFile(InputFileChangeEventArgs e)
        {
            string url = $"https://localhost:7146/api/loadfile{this.OthelloRoomNumber}";

            var file = e.GetMultipleFiles(1).FirstOrDefault();
            if (file == null)
            {
                return;
            }
            var buf = new byte[file.Size];
            await file.OpenReadStream().ReadAsync(buf);
            var logInfoString = System.Text.Encoding.UTF8.GetString(buf);
            var jsonString = JsonSerializer.Serialize(logInfoString);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            await MyHttpClient.PostAsync(url, content);
        }

        private async Task DownloadFileFromStream()
        {
            var fileStream = await GetFileStream();
            var fileName = "savedlog.txt";

            using var streamRef = new DotNetStreamReference(stream: fileStream);

            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
        private async Task<Stream> GetFileStream()
        {
            var logOfGameList = await FetchLogOnTheServer();
            var logLines = logOfGameList.Select((log) => $"{log.IsPass}@{log.Turn}@{log.Point.X}{log.Point.Y}");
            return new MemoryStream(Encoding.UTF8.GetBytes(String.Join(",", logLines)));
        }
    }

    public class RoomInformationForClient
    {
        public GameMode GameModeOfRoom;

        public Int32 NumberOfConnections;

        public RoomInformationForClient(GameMode gameMode, Int32 numberOfConnections)
        {
            GameModeOfRoom = gameMode;
            NumberOfConnections = numberOfConnections;
        }
    }

    public enum PlayerStatusInSelect { Nothing, Waiting, CantSelect }

    public class MyHttpClient
    {
        private static HttpClient client;

        static MyHttpClient()
        {
            client = new HttpClient();
        }

        public static async Task<HttpResponseMessage> GetAsync(String url)
        {
            return await client.GetAsync(url);
        }
        public static async Task<HttpResponseMessage> PostAsync(String url, HttpContent content)
        {
            return await client.PostAsync(url, content);
        }
    }

}

