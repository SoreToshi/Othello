using OthelloClassLibrary.Models;
using System.Text;
using System.Text.Json;

namespace MyOthelloClient.Models
{
    public static class HitApi
    {
        public static async Task<Dictionary<Int32, RoomInformationForClient>> FetchRoomsInformationForClient()
        {
            var fetchRoomUrl = "https://localhost:7146/api/fetchroomsinformationforclient";
            var result = await MyHttpClient.GetAsync(fetchRoomUrl);
            return  ParseRoomsInformationStringToDictionary(await result.Content.ReadAsStringAsync());
        }
        private static Dictionary<Int32, RoomInformationForClient> ParseRoomsInformationStringToDictionary( String roomsInformationString)
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

        public static async Task<IList<(Int32, Int32)>> FetchNumberOfConnections()
        {
            var fetchNumberOfConnectionsUrl = "https://localhost:7146/api/fetchnumberofconnections";
            var result = await MyHttpClient.GetAsync(fetchNumberOfConnectionsUrl);
            return ParseNumberOfConnectionStringToList(await result.Content.ReadAsStringAsync());
        }
        private static IList<(Int32, Int32)> ParseNumberOfConnectionStringToList(String numberOfConnectionOfRoomsString)
        {
            var numberOfConnectionOfRoomsArray = numberOfConnectionOfRoomsString.Split(',');
            List<(Int32 roomNumber, Int32 numberOfConnection)> numberOfConnectionList = new List<(Int32,Int32)>();
            foreach (var numberOfConnectionLine in numberOfConnectionOfRoomsArray)
            {
                var numberOfConnectionInfo = numberOfConnectionLine.Split('@');
                var roomNumber = Int32.Parse(numberOfConnectionInfo[0]);
                var numberOfConnection = Int32.Parse(numberOfConnectionInfo[1]);
                numberOfConnectionList.Add((roomNumber, numberOfConnection));
            }
            return numberOfConnectionList;
        }

        public static async void StartServerOthello(Int32 roomNumber , IPlayer player , String identificationNumber)
        {
            var StartUrl = $"https://localhost:7146/api/start{roomNumber}&{player.Turn}&{identificationNumber}";
            await MyHttpClient.GetAsync(StartUrl);
        }

        public static async void PutPiece(Int32 roomNumber, Int32 squareNumber)
        {
            var putPieceUrl = $"https://localhost:7146/api/putpiece{roomNumber}";
            var jsonString = JsonSerializer.Serialize(squareNumber);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            await MyHttpClient.PostAsync(putPieceUrl, content);
        }

        public static async Task<String> FetchIdentificationNumber(Int32 roomNumber)
        {
            var fetchIdentificationNumberUrl = $"https://localhost:7146/api/fetchidentificationnumber{roomNumber}";
            var result = await MyHttpClient.GetAsync(fetchIdentificationNumberUrl);

            return await result.Content.ReadAsStringAsync();
        }

        public static async Task<IList<LogOfGame>> FetchLogOnTheServer(Int32 othelloRoomNunber, String identificationNumber)
        {
            var result = await MyHttpClient.GetAsync($"https://localhost:7146/api/fetchlog{othelloRoomNunber}&{identificationNumber}");

            return ParseLogStringToLogList(await result.Content.ReadAsStringAsync());
        }
        private static IList<LogOfGame> ParseLogStringToLogList(String logString)
        {
            var logArr = logString.Split(',');
            var logOfGame = new List<LogOfGame>();

            if (IsLogExist(logString) == false)
            {
                return logOfGame;
            }

            return logArr.Select(OthelloClassLibrary.Models.LogOfGame.Parse).ToList();
        }
        private static Boolean IsLogExist(String logString)
        {
            var logInfos = logString.Split(',');
            var isPass = logInfos[0].Split('@')[0];
            return isPass == "False" || isPass == "True";
        }

        public static async Task<PlayerStatusInSelect> FetchPlayerStatus(Int32 othelloRoomNumber, Turn playerTurn, String identificationNumber)
        {
            var result = await MyHttpClient.GetAsync($"https://localhost:7146/api/fetchplayerstatus{othelloRoomNumber}&{playerTurn}&{identificationNumber}");
            return ParsePlayerStatusStrToPlayerStatusInSelect(await result.Content.ReadAsStringAsync());
        }
        private static PlayerStatusInSelect ParsePlayerStatusStrToPlayerStatusInSelect(String playerStatusStr)
        {
            if (IsPlayerStatusStringCorrect(playerStatusStr) == false)
            {
                return PlayerStatusInSelect.Nothing;
            }

            switch (playerStatusStr)
            {
                case "WaitOpponent":
                    return PlayerStatusInSelect.Waiting;
                case "AlreadySelected":
                    return PlayerStatusInSelect.CantSelect;
                case "Start":
                    return PlayerStatusInSelect.Nothing;

                default: return PlayerStatusInSelect.Nothing;
            }
        }
        private static Boolean IsPlayerStatusStringCorrect(String playerStatusString)
        {
            return playerStatusString == "WaitOpponent"
                || playerStatusString == "AlreadySelected"
                || playerStatusString == "Start";
        }
        public static async Task<String> FetchModeSelectOpponentAction(Int32 othelloRoomNumber, String identificationNumber)
        {
            var fetchOpponentUrl = $"https://localhost:7146/api/fetchopponentaction{othelloRoomNumber}&{identificationNumber}";
            var result = await MyHttpClient.GetAsync(fetchOpponentUrl);
            return await result.Content.ReadAsStringAsync();
        }
        public static async void RestartServerOthello(Int32 roomNumber)
        {
            await MyHttpClient.GetAsync($"https://localhost:7146/api/restart{roomNumber}");
        }
        public static async void RetireServerOthello(Int32 othelloRoomNumber , Turn turn)
        {
            await MyHttpClient.GetAsync($"https://localhost:7146/api/retire{othelloRoomNumber}&{turn}");
        }
    }

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
