using MyOthelloWeb.Models;
using MyOthelloWeb.Pages;
using Microsoft.AspNetCore.Mvc;
using DeepCopy;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Text;
using Microsoft.AspNetCore.Cors;

namespace MyOthelloWeb.Controllers
{
    [ApiController]
    [Route("/api")]
    public class MyOthelloController : ControllerBase
    {
        private readonly ILogger<MyOthelloController> _logger;


        public MyOthelloController(ILogger<MyOthelloController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("hello")]
        public String Hello() {
            return "hello world!";
        }

        [HttpPost]
        [Route("startfirst")]
        public void PlayerStartsFirst()
        {
            var player = new Human(Turn.First);
            var othello = OthelloManager.Instance;
            othello.SetPlayer(player);

            var cpuTurn = player.Turn == Turn.First ? Turn.Second : Turn.First;
            var playerCpu = new Cpu(cpuTurn);
            othello.SetPlayer(playerCpu);

            othello.ChangeGameState(GameState.MatchRemaining);
        }
        [HttpPost]
        [Route("startsecond")]
        public void PlayerStartsSecond()
        {
            var player = new Human(Turn.Second);
            var othello = OthelloManager.Instance;
            othello.SetPlayer(player);

            var cpuTurn = player.Turn == Turn.First ? Turn.Second : Turn.First;
            var playerCpu = new Cpu(cpuTurn);
            othello.SetPlayer(playerCpu);

            othello.ChangeGameState(GameState.MatchRemaining);
        }

        [Route("putpiece/{number}")]
        [HttpPost]
        [EnableCors]
        public void PutPiece(int number)
        {
            var othello = OthelloManager.Instance;
            if (othello.HasRightToPut() == false)
            {
                return;
            }
            othello.PutPiece(number);
        }

        [HttpPost]
        [Route("restart")]
        public void RestartOthello()
        {
            OthelloManager.Instance = new MyOthelloModel(OthelloManager.BoardSize, OthelloManager.Instance.ThemeColor);
        }

        [HttpPost]
        [Route("retire")]
        public void RetireMatch()
        {
            OthelloManager.Instance.ChangeGameState(GameState.MatchRetired);
        }

        [HttpPost]
        [Route("backfromlog/{numberoflog}")]
        public void BackFromLog(Int32 numberOfLog)
        {
            var othello = OthelloManager.Instance;
            othello.EraceLogFromSpecifiedTurn(numberOfLog);

            var previousLogOfGame = othello.Log.LogOfGame;
            var previousPlayerFirst = othello.PlayerFirst;
            var previousPlayerSecond = othello.PlayerSecond;

            this.RestartOthello();
            
            // OthelloManager.Instanceの中身がRestartOthelloで新しくなるため新しい変数に代入します。
            var newOthello = OthelloManager.Instance;
            newOthello.ReCreateOthelloSituation(previousLogOfGame, previousPlayerFirst, previousPlayerSecond);
        }

        [HttpPost]
        [Route("selectcolor/{color}")]
        public void SelectThemeColor(String color)
        {
            var themeColor = StringToOthelloTheme(color);
            
            OthelloManager.Instance.ThemeColor = themeColor;
        }

        public ThemeColor StringToOthelloTheme(String color)
        {
            switch (color)
            {
                case "Default":
                    return ThemeColor.Default;
                case "Dango":
                    return ThemeColor.Dango;
                case "Sakura":
                    return ThemeColor.Sakura;
                case "Ice":
                    return ThemeColor.Ice;
                default:
                    return ThemeColor.Default;
            }
        }

        [HttpPost]
        [Route("Post")]
        public void Post()
        {
            var player = new Human(Turn.First);
        }


        // テキストで送られてきたものをloadfileメソッドに会う形式に整えます。(新しいメソッドが必要かもしれません)
        [HttpPost]
        [Route("loadfile")]
        public void LoadLogFile([FromBody]String log)
        {
            this.RestartOthello();

            var othello = OthelloManager.Instance;

            var listOfLog = LoadFiles(log);
            othello.ReCreateOthelloSituation(listOfLog);

            othello.ChangeGameState(GameState.SelectSide);
        }

        // この部分はクライアント側で行われる処理なのでIndexに戻します。
        // またリストではなく、テキストで送るための形式に変更する必要があります。
        private IList<LogOfGame> LoadFiles(String log)
        {
            var listOfLog = new List<LogOfGame>();
            var logInfos = log.Split(',');
            foreach (var line in logInfos)
            {
                var splitLine = line.Split('@');
                var isPass = splitLine[0] == "true" ? true : false;
                var turn = splitLine[1] == "First" ? Turn.First : Turn.Second;
                var x = splitLine[2][0].ToString();
                var y = splitLine[2][1].ToString();
                var point = new Point(Int32.Parse(x), Int32.Parse(y));
                listOfLog.Add(new Models.LogOfGame(isPass, turn, point));
            }
            return listOfLog;
        }

        [HttpPost]
        [Route("download")]
        public String FetchLogFile()
        {
            var logLines = OthelloManager.Instance.Log.LogOfGame.Select((log) => $"{log.IsPass}@{log.Turn}@{log.Point.X}{log.Point.Y}");
            var logString = String.Join(",", logLines);
            return logString;
        }
    }
}