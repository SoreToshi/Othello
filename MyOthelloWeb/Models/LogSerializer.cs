using OthelloClassLibrary.Models;

namespace MyOthelloWeb.Models
{
    public static class LogSerializer
    {
        public static String Serialize(IList<LogOfGame> logOfGame) {
            var logLines = logOfGame.Select((log) => $"{log.IsPass}@{log.Turn}@{log.Point.X}{log.Point.Y}");
            return String.Join(",", logLines);
        }

        public static IList<LogOfGame> Deserialize(String log) {
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
