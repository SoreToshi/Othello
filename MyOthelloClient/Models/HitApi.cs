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
            return ParseRoomsInformationStringToDictionary(await result.Content.ReadAsStringAsync());
        }

        public static async Task<RoomInformationForClient> FetchRoomInformationForClient(Int32 roomNumber)
        {
            var rooms = await FetchRoomsInformationForClient();
            return rooms[roomNumber];
        }

        private static Dictionary<Int32, RoomInformationForClient> ParseRoomsInformationStringToDictionary(String roomsInformationString)
        {
            return roomsInformationString.Split(',').Select(roomInfo =>
            {
                var roomInfoLine = roomInfo.Split('@');
                var roomNumber = Int32.Parse(roomInfoLine[0]);
                var gameMode = roomInfoLine[1] == "VsHuman" ? GameMode.VsHuman : GameMode.VsCpu;
                var connectionCount = Int32.Parse(roomInfoLine[2]);
                return new { roomNumber, gameMode, connectionCount };
            }).ToDictionary((roomInfo) =>
            {
                return roomInfo.roomNumber;
            }, (roomInfo) =>
            {
                return new RoomInformationForClient(roomInfo.gameMode, roomInfo.connectionCount);
            });
        }

        public static async Task<IList<(Int32, Int32)>> FetchNumberOfConnections()
        {
            var fetchNumberOfConnectionsUrl = "https://localhost:7146/api/fetchnumberofconnections";
            var result = await MyHttpClient.GetAsync(fetchNumberOfConnectionsUrl);
            return ParseNumberOfConnectionStringToList(await result.Content.ReadAsStringAsync());
        }

        private static IList<(Int32, Int32)> ParseNumberOfConnectionStringToList(String numberOfConnectionOfRoomsString)
        {
            return numberOfConnectionOfRoomsString.Split(',').Select((numberOfConnectionLine) =>
            {
                var numberOfConnectionInfo = numberOfConnectionLine.Split('@');
                var roomNumber = Int32.Parse(numberOfConnectionInfo[0]);
                var numberOfConnection = Int32.Parse(numberOfConnectionInfo[1]);
                return (roomNumber, numberOfConnection);
            }).ToList();
        }

        public static async void StartServerOthello(Int32 roomNumber, IPlayer player, String identificationNumber)
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
            var content = await result.Content.ReadAsStringAsync();
            return ParseLogStringToLogList(content);
        }

        private static IList<LogOfGame> ParseLogStringToLogList(String logString)
        {
            var logs = logString.Split(',');
            var logOfGame = new List<LogOfGame>();

            if (IsLogExist(logString) == false)
            {
                return logOfGame;
            }

            return logs.Select(OthelloClassLibrary.Models.LogOfGame.Parse).ToList();
        }

        private static Boolean IsLogExist(String logString)
        {
            var logInfos = logString.Split(',');
            var isPass = logInfos[0].Split('@')[0];
            return new string[] { "True", "False" }.Contains(isPass);
        }

        public static async Task<PlayerStatusInSelect> FetchPlayerStatus(Int32 othelloRoomNumber, Turn playerTurn, String identificationNumber)
        {
            var result = await MyHttpClient.GetAsync($"https://localhost:7146/api/fetchplayerstatus{othelloRoomNumber}&{playerTurn}&{identificationNumber}");
            var content = await result.Content.ReadAsStringAsync();
            return ParsePlayerStatusStrToPlayerStatusInSelect(content);
        }

        private static PlayerStatusInSelect ParsePlayerStatusStrToPlayerStatusInSelect(String playerStatus)
        {
            switch (playerStatus)
            {
                case "WaitOpponent":
                    return PlayerStatusInSelect.Waiting;
                case "AlreadySelected":
                    return PlayerStatusInSelect.CantSelect;
                case "Start":
                    return PlayerStatusInSelect.Nothing;
                default:
                    return PlayerStatusInSelect.Nothing;
            }
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

        public static async void RetireServerOthello(Int32 othelloRoomNumber, Turn turn)
        {
            await MyHttpClient.GetAsync($"https://localhost:7146/api/retire{othelloRoomNumber}&{turn}");
        }
    }

    public class MyHttpClient
    {
        private static HttpClient client = new HttpClient();

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
