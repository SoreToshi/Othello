using OthelloClassLibrary.Models;
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
        [Route("fugastart{Turn}")]
        public void FugaStartSecond(String turn)
        {
            if (turn != "first" && turn != "second")
            {
                return;
            }

            Human player;
            var othello = OthelloManager.Instance; ;
            if (turn == "first")
            {
                player = new Human(Turn.First);
            }
            else
            {
                player = new Human(Turn.Second);
            }
            othello.SetPlayer(player);
            var cpuTurn = player.Turn == Turn.First ? Turn.Second : Turn.First;
            var playerCpu = new Cpu(cpuTurn);
            othello.SetPlayer(playerCpu);

            othello.ChangeGameState(GameState.MatchRemaining);
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
        [HttpGet]
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

        [Route("putpiece/{squarenumber}")]
        [HttpGet]
        public void PutPiece(Int32 squareNumber)
        {
            var othello = OthelloManager.Instance;
            if (othello.HasRightToPut() == false)
            {
                return;
            }
            othello.PutPiece(squareNumber);
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
        public String SelectThemeColor(String color)
        {
            var themeColor = StringToOthelloTheme(color);
            
            OthelloManager.Instance.ThemeColor = themeColor;

            return color;
        }

        public ThemeColor StringToOthelloTheme(String color)
        {
            switch (color)
            {
                case "default":
                    return ThemeColor.Default;
                case "dango":
                    return ThemeColor.Dango;
                case "sakura":
                    return ThemeColor.Sakura;
                case "ice":
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

        [HttpGet]
        [Route("returnlog")]
        public String PlayerStartsSecondAndReturn()
        {
            var othello = OthelloManager.Instance;
            var logLines = OthelloManager.Instance.Log.LogOfGame.Select((log) => $"{log.IsPass}@{log.Turn}@{log.Point.X}{log.Point.Y}");
            var logString = String.Join(",", logLines);
            return logString;
        }


        [HttpPost]
        [Route("loadfile")]
        public void LoadLogFile([FromBody] String logString)
        {
            this.RestartOthello();

            var othello = OthelloManager.Instance;

            var listOfLog = LoadFiles(logString);
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