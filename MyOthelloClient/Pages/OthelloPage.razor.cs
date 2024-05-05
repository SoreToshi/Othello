using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MyOthelloClient.Models;
using OthelloClassLibrary.Models;
using System.Text;
using System.Text.Json;

namespace MyOthelloClient.Pages
{
    public partial class OthelloPage
    {
        private readonly ClientManager ClientManager = new ClientManager();
        private MyOthelloModel Othello
        {
            get
            {
                return ClientManager.Model;
            }
        }
        private Int32 OthelloRoomNumber
        {
            get
            {
                return ClientManager.OthelloRoomNumber;
            }
            set
            {
                ClientManager.OthelloRoomNumber = value;
            }
        }

        private String ID
        {
            get
            {
                return this.ClientManager.ID;
            }
        }


        private Dictionary<Int32, RoomInformationForClient> OthelloRooms
        {
            get
            {
                return this.ClientManager.OthelloRooms;
            }
        }

        private Turn MyTurn
        {
            get
            {
                return this.ClientManager.MyTurn;
            }
        }
        private PlayerStatusInSelect ModeSelectPlayerStatus
        {
            get
            {
                return ClientManager.PlayerStatusInSelect;
            }
            set
            {
                ClientManager.PlayerStatusInSelect = value;
            }
        }
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

        private event Action UpdateNumberOfConnectionEvent;
        private event Action<Int32> SelectOthelloRoomEvent;
        private event Action MoveToRoomSelectEvent;

        public OthelloPage()
        {
            this.SetViewEventInOthello();

            ClientManager.PlayerStatusInSelectChangedEvent += (playerStatusInSelect) => this.StateHasChanged();
            this.ClientManager.UpdateRoomsInformationEvent += this.StateHasChanged;
            ClientManager.RecreateOthelloEvent += this.SetViewEventInOthello;

            this.SelectOthelloRoomEvent += (roomNumber) => this.StateHasChanged();
            this.UpdateNumberOfConnectionEvent += this.StateHasChanged;
            this.MoveToRoomSelectEvent += this.UpdateNumberOfConnections;

            this.ClientManager.UpdateRoomsInformation();
        }

        private void SetViewEventInOthello()
        {
            this.Othello.GameStateChangedEvent += (state, turn) => this.StateHasChanged();
            this.Othello.TurnEndEvent += this.StateHasChanged;
            this.Othello.RecreateOthelloSituationEvent += this.StateHasChanged;
        }
        private async void UpdateNumberOfConnections()
        {
            this.ClientManager.UpdateNumberOfConnections(await HitApi.FetchNumberOfConnections());
            this.UpdateNumberOfConnectionEvent?.Invoke();
        }

        private async void SelectOthelloRoom(Int32 roomNumber)
        {
            if (!await this.ClientManager.IsTheRoomSelectable(roomNumber))
            {
                return;
            }

            // IDを受け取る前にIDを必要とする行程に進んでしまうためディレイをかけて待ちます。
            this.ClientManager.FetchID(roomNumber);
            await Task.Delay(100);
            this.OthelloRoomNumber = roomNumber;

            var polling = new Polling();
            polling.PollingToReflectServerSituation(this.OthelloRoomNumber, this.ID);

            this.SelectOthelloRoomEvent?.Invoke(OthelloRoomNumber);
        }


        private void StartGame(IPlayer player)
        {
            if (this.ModeSelectPlayerStatus == PlayerStatusInSelect.Waiting)
            {
                return;
            }
            this.ClientManager.BuildModeSelectPlayerSituation(this.OthelloRoomNumber, player);

            HitApi.StartServerOthello(this.OthelloRoomNumber, player, this.ID);
        }

        private void PutPiece(Int32 squareNumber)
        {
            if (this.GameState != GameState.MatchRemaining)
            {
                return;
            }
            if (this.MyTurn != this.Othello.Turn)
            {
                return;
            }

            HitApi.PutPiece(this.OthelloRoomNumber, squareNumber);
        }

        private void RestartMatch()
        {
            HitApi.RestartServerOthello(this.OthelloRoomNumber);
        }

        private void RetireMatch()
        {
            if (this.GameState != GameState.MatchRemaining)
            {
                return;
            }
            HitApi.RetireServerOthello(this.OthelloRoomNumber, this.MyTurn);
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
            ClientManager.RecreateOthelloModel();

            // ルームセレクトの時自身のオセロルームナンバーを初期値の0に戻します。
            this.OthelloRoomNumber = 0;
            this.ModeSelectPlayerStatus = PlayerStatusInSelect.Nothing;

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
            var logOfGameList = await HitApi.FetchLogOnTheServer(this.OthelloRoomNumber, this.ID);
            var logLines = logOfGameList.Select((log) => $"{log.IsPass}@{log.Turn}@{log.Point.X}{log.Point.Y}");
            return new MemoryStream(Encoding.UTF8.GetBytes(String.Join(",", logLines)));
        }
    }
}

