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

        private IDictionary<Int32, Int32> RoomCapacity
        {
            get
            {
                return this.ClientManager.OthelloRooms.ToDictionary(othelloRooms => othelloRooms.Key, othelloRooms => othelloRooms.Value.GameModeOfRoom == GameMode.VsHuman ? 2 : 1);
            }
        }

        private String OthelloTheme { get; set; } = "default";

        private event Action UpdateNumberOfConnectionEvent;
        private event Action<Int32> SelectOthelloRoomEvent;
        private event Action MoveToRoomSelectEvent;

        public OthelloPage()
        {
            this.ClientManager.OnChangeState += this.StateHasChanged;

            this.SelectOthelloRoomEvent += (roomNumber) => this.StateHasChanged();
            this.UpdateNumberOfConnectionEvent += this.StateHasChanged;
            this.MoveToRoomSelectEvent += this.UpdateNumberOfConnections;

            this.ClientManager.UpdateRoomsInformation();
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
            this.ClientManager.OthelloRoomNumber = roomNumber;

            var polling = new Polling();
            polling.ReflectServerSituation(this.ClientManager, (logOfGameList) => {
                ClientManager.RecreateOthelloModel();
                this.ClientManager.ReCreateOthelloSituation(logOfGameList);
            });

            this.SelectOthelloRoomEvent?.Invoke(this.ClientManager.OthelloRoomNumber);
        }


        private void StartGame(IPlayer player)
        {
            if (this.ClientManager.PlayerStatusInSelect == PlayerStatusInSelect.Waiting)
            {
                return;
            }
            this.ClientManager.BuildModeSelectPlayerSituation(this.ClientManager.OthelloRoomNumber, player);

            HitApi.StartServerOthello(this.ClientManager.OthelloRoomNumber, player, this.ClientManager.Id);
        }

        private void PutPiece(Int32 squareNumber)
        {
            if (this.ClientManager.GameState != GameState.MatchRemaining)
            {
                return;
            }
            if (this.ClientManager.MyTurn != this.ClientManager.CurrentTurn)
            {
                return;
            }

            HitApi.PutPiece(this.ClientManager.OthelloRoomNumber, squareNumber);
        }

        private void RestartMatch()
        {
            HitApi.RestartServerOthello(this.ClientManager.OthelloRoomNumber);
        }

        private void RetireMatch()
        {
            if (this.ClientManager.GameState != GameState.MatchRemaining)
            {
                return;
            }
            HitApi.RetireServerOthello(this.ClientManager.OthelloRoomNumber, this.ClientManager.MyTurn);
        }

        private async void BackFromLog(Int32 numberOfLog)
        {
            string url = $"https://localhost:7146/api/backfromlog{this.ClientManager.OthelloRoomNumber}";
            var jsonString = JsonSerializer.Serialize(numberOfLog);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            await MyHttpClient.PostAsync(url, content);
        }

        private void MoveToRoomSelect()
        {
            ClientManager.RecreateOthelloModel();

            // ルームセレクトの時自身のオセロルームナンバーを初期値の0に戻します。
            this.ClientManager.OthelloRoomNumber = 0;
            this.ClientManager.PlayerStatusInSelect = PlayerStatusInSelect.Nothing;

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
            string url = $"https://localhost:7146/api/loadfile{this.ClientManager.OthelloRoomNumber}";

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
            var logOfGameList = await HitApi.FetchLogOnTheServer(this.ClientManager.OthelloRoomNumber, this.ClientManager.Id);
            var logLines = logOfGameList.Select((log) => $"{log.IsPass}@{log.Turn}@{log.Point.X}{log.Point.Y}");
            return new MemoryStream(Encoding.UTF8.GetBytes(String.Join(",", logLines)));
        }
    }
}

