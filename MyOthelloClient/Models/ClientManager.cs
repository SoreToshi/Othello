using NLog.LayoutRenderers.Wrappers;
using OthelloClassLibrary.Models;

namespace MyOthelloClient.Models
{
    public class ClientManager
    {
        public static MyOthelloModel Model { get; private set; } = new MyOthelloModel(8, ThemeColor.Default);
        public static Int32 OthelloRoomNumber { get; set; }

        // ルーム入室時にサーバーから割り振られます。
        public String ID { get; private set; } = String.Empty;

        public Dictionary<Int32, RoomInformationForClient> OthelloRooms { get; private set; } = new Dictionary<Int32, RoomInformationForClient> {
            {0, new RoomInformationForClient(GameMode.VsHuman, 0)}
        };

        public static PlayerStatusInSelect _PlayerStatusInSelect = PlayerStatusInSelect.Nothing;
        public static PlayerStatusInSelect PlayerStatusInSelect
        {
            get
            {
                return _PlayerStatusInSelect;
            }
            set
            {
                _PlayerStatusInSelect = value;
                PlayerStatusInSelectChangedEvent?.Invoke(value);
            }
        }


        public Turn MyTurn { get; private set; }

        public static event Action<PlayerStatusInSelect> PlayerStatusInSelectChangedEvent;
        public event Action UpdateRoomsInformationEvent;
        public static event Action RecreateOthelloEvent;

        public async void UpdateRoomsInformation()
        {
            var roomsInformation = await HitApi.FetchRoomsInformationForClient();
            this.OthelloRooms = roomsInformation;
            this.UpdateRoomsInformationEvent.Invoke();
        }

        public void UpdateNumberOfConnections(IList<(Int32 roomNumber, Int32 numberOfConnection)> numberOfConnectionList)
        {
            foreach (var numberOfConnectionInfo in numberOfConnectionList)
            {
                this.OthelloRooms[numberOfConnectionInfo.roomNumber].NumberOfConnections = numberOfConnectionInfo.numberOfConnection;
            }
        }

        public async Task<Boolean> IsTheRoomSelectable(Int32 roomNumber)
        {
            return roomNumber > 0 && roomNumber <= OthelloRooms.Count 
                ? !await this.IsSelectRoomFull(roomNumber) 
                : false;
        }
        private async Task<Boolean> IsSelectRoomFull(Int32 roomNumber)
        {
            var roomsInformation = await HitApi.FetchRoomsInformationForClient();
            var roomInformation = roomsInformation[roomNumber];

            // VsHumanの部屋最大人数は2、VsCpuは1です。
            return roomInformation.GameModeOfRoom == GameMode.VsHuman
                ? roomInformation.NumberOfConnections >= 2
                : roomInformation.NumberOfConnections >= 1;
        }

        public async void FetchID(Int32 roomNumber)
        {
            this.ID = await HitApi.FetchIdentificationNumber(roomNumber);
        }

        public static void RecreateOthelloModel()
        {
            Model = new MyOthelloModel(8, Model.ThemeColor);
            RecreateOthelloEvent.Invoke();
        }

        public async void BuildModeSelectPlayerSituation(Int32 roomNumber, IPlayer player)
        {
            var playerStatus = await HitApi.FetchPlayerStatus(roomNumber, player.Turn, this.ID);

            switch (playerStatus)
            {
                case PlayerStatusInSelect.Waiting:
                    PlayerStatusInSelect = PlayerStatusInSelect.Waiting;
                    this.MyTurn = player.Turn;

                    var polling = new Polling();
                    polling.PollingToWaitOpponent(roomNumber, ID);

                    break;

                case PlayerStatusInSelect.CantSelect:
                    PlayerStatusInSelect = PlayerStatusInSelect.CantSelect;
                    break;

                case PlayerStatusInSelect.Nothing:
                    PlayerStatusInSelect = PlayerStatusInSelect.Nothing;
                    this.MyTurn = player.Turn;
                    Model.ChangeGameState(GameState.MatchRemaining);
                    break;
            }
        }
    }

    public class RoomInformationForClient
    {
        public GameMode GameModeOfRoom { get; private set; }

        public Int32 NumberOfConnections {  get; set; }

        public RoomInformationForClient(GameMode gameMode, Int32 numberOfConnections)
        {
            this.GameModeOfRoom = gameMode;
            this.NumberOfConnections = numberOfConnections;
        }
    }

    public enum PlayerStatusInSelect { Nothing, Waiting, CantSelect }
}
