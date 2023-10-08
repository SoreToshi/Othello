using Microsoft.AspNetCore.Components.Forms;
using OthelloClassLibrary.Models;
using System.Text;
using System.Text.Json;
using Timer = System.Timers.Timer;

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
        private event Action<GameState> RetireGameEvent;
        private event Action<GameState> RestartServerOthelloEvent;
        private event Action<GameState> RestartClientOthelloEvent;

        public OthelloPage()
        {
            this.BoardSize = 8;
            this.Othello = OthelloManager.Instance;
            this.Othello.TurnEndEvent += this.StateHasChanged;
            this.OnLoadLogFileEvent += this.StateHasChanged;
            this.RetireGameEvent += (gameState) => this.StateHasChanged();
            this.RestartServerOthelloEvent += (gameState) => this.ChangeStateToTheStartingPoint();
            this.RestartClientOthelloEvent += (gameState) => this.StateHasChanged();
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
            string url = $"https://localhost:7146/api/start{queryTurn}";

            await client.GetAsync(url);
            this.StartGameEvent(player);
        }
        private async void PutPiece(Int32 squareNumber)
        {
            if (this.GameState != GameState.MatchRemaining)
            {
                return;
            }

            var client = new MyHttpClinet();

            string url = "https://localhost:7146/api/putpiece";

            var jsonString = JsonSerializer.Serialize(squareNumber);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            await client.PostAsync(url, content);
        }

        private async void PollingToReflectServerSituation()
        {
            // 処理を軽くするためサーバーとクライアントのLogに差がないときはオセロを作り直さないようにします。
            // ゲーム開始直後だけはLogがサーバーと変わりがなくても作り直す処理が入ります。
            var logOfGameList = LogStringToLogList(await SearchLogOnTheServer());
            this.BuildSituationFromLog(logOfGameList);

            var pollingToReflectRate = new Timer(100); // msec

            pollingToReflectRate.Elapsed += async (sender, e) =>
            {
                if (this.GameState == GameState.SelectSide)
                {
                    pollingToReflectRate.Stop();
                    return;
                }

                var logOfGameList = this.LogStringToLogList(await this.SearchLogOnTheServer());
                if (this.IsLogUpDated(logOfGameList) == false)
                {
                    return;
                }

                if (this.IsRetired(logOfGameList))
                {
                    this.RetireProcess(logOfGameList);
                    return;
                }

                this.BuildSituationFromLog(logOfGameList);
            };

            pollingToReflectRate.Start();
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
                var turn = splitLine[1] == "First" ? Turn.First : Turn.Second;
                String x;
                String y;
                if (splitLine[2][0].ToString() == "-")
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
        public async Task<String> SearchLogOnTheServer()
        {
            var client = new MyHttpClinet();
            var result = await client.GetAsync("https://localhost:7146/api/returnlog");
            var logInfo = await result.Content.ReadAsStringAsync();
            return logInfo;
        }
        public Boolean IsLogExist(String logString)
        {
            var logInfos = logString.Split(',');
            var isPass = logInfos[0].Split('@')[0];
            return isPass == "False" || isPass == "True" ? true : false;
        }
        private Boolean IsLogUpDated(IList<LogOfGame> logOfGame)
        {
            return this.Othello.Log.LogOfGame.Count != logOfGame.Count;
        }
        private Boolean IsRetired(IList<LogOfGame> logOfGameList)
        {
            if (logOfGameList.Count == 0)
            {
                return false;
            }
            // Retireした時にサーバーのLogではPoint(-5,-5)として記録されます。
            return logOfGameList.Last().Point.X == -5;
        }
        private void RetireProcess(IList<LogOfGame> logOfGameList)
        {
            logOfGameList.RemoveAt(logOfGameList.Count - 1);
            this.Othello.ChangeGameState(GameState.MatchRetired);
            this.RetireGameEvent(GameState.MatchRetired);
        }
        private void BuildSituationFromLog(IList<LogOfGame> logOfGameList)
        {

            this.RestartOthello();

            this.Othello.ReCreateOthelloSituation(logOfGameList);

            this.OnLoadLogFileEvent?.Invoke();
        }

        public async void BackFromLog(Int32 numberOfLog)
        {
            var client = new MyHttpClinet();
            string url = "https://localhost:7146/api/backfromlog";
            var jsonString = JsonSerializer.Serialize(numberOfLog);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            await client.PostAsync(url, content);
        }

        public void RestartOthello()
        {
            this.Othello = new MyOthelloModel(this.BoardSize, this.Othello.ThemeColor);
            this.Othello.TurnEndEvent += this.StateHasChanged;
        }

        public async void RestartServerOthello()
        {
            if (this.GameState == GameState.SelectSide)
            {
                return;
            }
            var client = new MyHttpClinet();
            await client.GetAsync("https://localhost:7146/api/restart");
            this.RestartServerOthelloEvent(GameState.SelectSide);
        }

        private void ChangeStateToTheStartingPoint()
        {
            var checkLogTimer = new Timer(100); // msec

            checkLogTimer.Elapsed += (sender, e) =>
            {
                if (this.LogOfGame.Count != 0)
                {
                    return;
                }

                this.Othello.ChangeGameState(GameState.SelectSide);
                this.RestartClientOthelloEvent(GameState.SelectSide);
                checkLogTimer.Stop();
            };

            checkLogTimer.Start();
        }

        public async void RetireMatch()
        {
            var client = new MyHttpClinet();
            await client.GetAsync("https://localhost:7146/api/retire");
        }


        public void ChangeTheme(ThemeColor color)
        {
            var stringOfColor = this.OthelloThemeToString(color);
            this.OthelloTheme = stringOfColor;
        }
        public String OthelloThemeToString(ThemeColor themeColor)
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


        public async void UpLoadLogFile(InputFileChangeEventArgs e)
        {
            this.RestartOthello();

            var client = new MyHttpClinet();
            string url = "https://localhost:7146/api/loadfile";

            var file = e.GetMultipleFiles(1);
            var buf = new byte[file[0].Size];
            await file[0].OpenReadStream().ReadAsync(buf);
            var logInfoString = System.Text.Encoding.UTF8.GetString(buf);
            var jsonString = JsonSerializer.Serialize(logInfoString);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            await client.PostAsync(url, content);

            // アップロードしたファイルがサーバーで反映されるまで待機します。
            var loadedLogOfGame = this.LogStringToLogList(logInfoString);
            var logOfGameOnTheServer = this.LogStringToLogList(await this.SearchLogOnTheServer());
            var SearchLogTimer = new Timer(100); // msec
            SearchLogTimer.Elapsed += async (sender, e) =>
            {
                logOfGameOnTheServer = this.LogStringToLogList(await this.SearchLogOnTheServer());
                while (loadedLogOfGame.Count > logOfGameOnTheServer.Count)
                {
                    return;
                }
                SearchLogTimer.Stop();
            };
            SearchLogTimer.Start();


            this.Othello.ReCreateOthelloSituation(logOfGameOnTheServer);
            this.Othello.ChangeGameState(GameState.SelectSide);

            this.OnLoadLogFileEvent?.Invoke();
        }
        private async Task<IList<LogOfGame>> FindServerLog()
        {
            return this.LogStringToLogList(await this.SearchLogOnTheServer());
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

