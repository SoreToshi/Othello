using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OthelloClassLibrary.Models;
using System;
using System.IO;
using System.Net.Mime;
using System.Reflection.Metadata;
using System.Security.AccessControl;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Timer = System.Timers.Timer;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;

namespace MyOthelloClient.Pages
{
    public partial class OthelloPage
    {
        private readonly Int32 BoardSize;
        private MyOthelloModel Othello;

        public Int32 NumberOfBlackPiece
        {
            get
            {
                return this.Othello.NumberOfBlackPiece;
            }
        }
        public Int32 NumberOfWhitePiece
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
        public GameState GameState
        {
            get
            {
                return this.Othello.GameState;
            }
        }
        public IList<Int32> SquareNumberListCanBePut
        {
            get
            {
                return this.Othello.SquareNumberListCanbePut;
            }
        }
        public IList<ThemeColor> ThemeColorList
        {
            get
            {
                return this.Othello.ThemeColorList;
            }
        }
        public IEnumerable<Tuple<OthelloPiece, Int32>> Pieces
        {
            get
            {
                return this.Othello.Pieces.Select((piece, i) => Tuple.Create(piece, i));
            }
        }
        public String OthelloTheme = "default";

        private event Action OnLoadLogFileEvent;
        private event Action<IPlayer> StartGameEvent;

        public OthelloPage()
        {
            this.BoardSize = 8;
            this.Othello = OthelloManager.Instance;
            this.Othello.TurnEndEvent += this.StateHasChanged;
            this.OnLoadLogFileEvent += this.StateHasChanged;
            this.StartGameEvent += (player) => this.PollingToReflectServerSituation();
        }

        private async void StartGame(IPlayer player)
        {
            String queryTurn = "first";
            if (player.Turn == Turn.Second)
            {
                queryTurn = "second";
            }
            var client = new MyHttpClinet();
            string url = $"https://localhost:7146/api/fugastart{queryTurn}";

            await client.GetAsync(url);
            this.StartGameEvent(player);
        }
        private async void PutPiece(Int32 squareNumber)
        {
            var client = new MyHttpClinet();
            string url = $"https://localhost:7146/api/putpiece/{squareNumber}";
            await client.GetAsync(url);
        }

        private async void PollingToReflectServerSituation()
        {
            var logOfGameList = LogStringToLogList(await SearchLogOnTheServer());

            this.BuildSituationFromLog(logOfGameList);

            var pollingRate = new Timer(100); // msec

            pollingRate.Elapsed += async (sender, e) =>
            {
                if (this.GameState != GameState.MatchRemaining)
                {
                    pollingRate.Stop();
                    return;
                }
                var logOfGameList = LogStringToLogList(await SearchLogOnTheServer());

                if (IsLogUpDated(logOfGameList) == false)
                {
                    return;
                }
                this.BuildSituationFromLog(logOfGameList);
            };

            pollingRate.Start();
        }
        private Boolean IsLogUpDated(IList<LogOfGame> logOfGame)
        {
            return this.Othello.Log.LogOfGame.Count != logOfGame.Count;
        }

        private void BuildSituationFromLog(IList<LogOfGame> logOfGameList)
        {
            var previousPlayerFirst = this.Othello.PlayerFirst;
            var previousPlayerSecond = this.Othello.PlayerSecond;

            this.RestartOthello();

            this.Othello.ReCreateOthelloSituation(logOfGameList, new Human(Turn.First), new Human(Turn.Second));

            this.OnLoadLogFileEvent?.Invoke();
        }
        public IList<LogOfGame> LogStringToLogList(String logString)
        {
            var logInfos = logString.Split(',');
            var logOfGame = new List<LogOfGame>();
            if (this.IsLogExist(logString) == false)
            {
                return logOfGame;
            }
            foreach (var line in logInfos)
            {
                var splitLine = line.Split('@');
                var isPass = splitLine[0] == "True" ? true : false;
                // これをつけると最後まで動くが
                if (isPass == true)
                {
                    continue;
                }
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
                logOfGame.Add(new LogOfGame(isPass, turn, point));
            }
            return logOfGame;
        }
        public Boolean IsLogExist(String logString)
        {
            var logInfos = logString.Split(',');
            var isPass = logInfos[0].Split('@')[0];
            return isPass == "False" || isPass == "True" ? true : false;
        }

        public async void FugaLoad(Task<String> log)
        {
            var previousPlayerFirst = this.Othello.PlayerFirst;
            var previousPlayerSecond = this.Othello.PlayerSecond;

            this.RestartOthello();
            var logString = await log;
            var logInfos = logString.Split(',');

            var logOfGame = new List<LogOfGame>();

            foreach (var line in logInfos)
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
                logOfGame.Add(new LogOfGame(isPass, turn, point));
            }


            this.Othello.ReCreateOthelloSituation(logOfGame, new Human(Turn.First), new Human(Turn.Second));

            this.Othello.ChangeGameState(GameState.SelectSide);

            this.OnLoadLogFileEvent?.Invoke();
        }
        public async Task<String> SearchLogOnTheServer()
        {
            var client = new MyHttpClinet();
            var result = await client.GetAsync("https://localhost:7146/api/returnlog");
            var logInfo = await result.Content.ReadAsStringAsync();
            return logInfo;
        }
        public void ChangeTheme(ThemeColor color)
        {
            var stringOfColor = this.FugaOthelloThemeToString(color);
            this.OthelloTheme = stringOfColor;
        }
        public String FugaOthelloThemeToString(ThemeColor themeColor)
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
        public void BackFromLog(Int32 numberOfLog)
        {
            this.Othello.EraceLogFromSpecifiedTurn(numberOfLog);

            // RestartOthello()でLogOfGame,PlayerFirst,PlayerSecondが初期化されてしまうので事前に代入する
            var previousLogOfGame = this.LogOfGame;
            var previousPlayerFirst = this.Othello.PlayerFirst;
            var previousPlayerSecond = this.Othello.PlayerSecond;

            this.RestartOthello();
            this.Othello.ReCreateOthelloSituation(previousLogOfGame, previousPlayerFirst, previousPlayerSecond);
        }
        public void RestartOthello()
        {
            this.Othello = new MyOthelloModel(this.BoardSize, Othello.ThemeColor);
            this.Othello.TurnEndEvent += this.StateHasChanged;
        }
        public void RetireMatch()
        {
            this.Othello.ChangeGameState(GameState.MatchRetired);
        }


        public async void UpLoadLogFile(InputFileChangeEventArgs e)
        {
            this.RestartOthello();

            var file = e.GetMultipleFiles(1);
            var buf = new byte[file[0].Size];
            await file[0].OpenReadStream().ReadAsync(buf);
            var logInfoString = System.Text.Encoding.UTF8.GetString(buf);

            string url = "https://localhost:7146/api/loadfile";

            var client = new MyHttpClinet();
            var content = new StringContent(logInfoString, Encoding.UTF8);

            var response = await client.PostAsync(url, content);

            var loadedLogOfGame = LogStringToLogList(logInfoString);
            var logOfGameStringOnTheServer = SearchLogOnTheServer();
            var logOfGameOnTheServer = LogStringToLogList(await logOfGameStringOnTheServer);

            while (loadedLogOfGame.Count != logOfGameOnTheServer.Count)
            {
                logOfGameOnTheServer = LogStringToLogList(await SearchLogOnTheServer());
            }

            this.Othello.ReCreateOthelloSituation(logOfGameOnTheServer, new Human(Turn.First), new Human(Turn.Second));
            this.Othello.ChangeGameState(GameState.SelectSide);

            this.OnLoadLogFileEvent?.Invoke();
        }
        public async Task DownloadFileFromStream()
        {

            var fileStream = GetFileStream();
            var fileName = "savedlog.txt";

            using var streamRef = new DotNetStreamReference(stream: fileStream);

            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
        private Stream GetFileStream()
        {
            var logLines = this.LogOfGame.Select((log) => $"{log.IsPass}@{log.Turn}@{log.Point.X}{log.Point.Y}");
            return new MemoryStream(Encoding.UTF8.GetBytes(String.Join(",", logLines)));
        }

        private void StateHasChangedForTest()
        {
            StateHasChanged();
        }


    }
    public class MyHttpClinet
    {
        // アクセス修飾子がprivateのstatic変数に生成したインスタンスを保存する
        private static HttpClient _client;

        // コンストラクタのアクセス修飾子をprivateにする
        static MyHttpClinet()
        {
            // 初期化処理
            _client = new HttpClient();
        }

        public async Task<HttpResponseMessage> GetAsync(String url)
        {
            var res = await _client.GetAsync($"{url}");
            return res;
        }
        public async Task<HttpResponseMessage> PostAsync(String url, HttpContent content)
        {
            var res = await _client.PostAsync($"{url}", content);
            return res;
        }

    }
}

