using NLog.LayoutRenderers.Wrappers;
using OthelloClassLibrary.Models;

namespace MyOthelloClient.Models
{
    public class ClientManager
    {
        private MyOthelloModel Model;

        public Int32 OthelloRoomNumber { get; set; }

        public IEnumerable<OthelloPiece> Pieces
        {
            get
            {
                return this.Model.Pieces;
            }
        }

        public IEnumerable<Int32> SquareNumberListCanbePut
        {
            get
            {
                return this.Model.SquareNumberListCanbePut;
            }
        }

        public GameState GameState
        {
            get
            {
                return this.Model.GameState;
            }
        }

        public IEnumerable<LogOfGame> LogOfGame
        {
            get
            {
                return this.Model.Log.LogOfGame;
            }
        }

        public Int32 LogOfGameCount
        {
            get
            {
                return this.Model.Log.LogOfGame.Count;
            }
        }

        public Int32 BlackPieceCount
        {
            get
            {
                return this.Model.NumberOfBlackPiece;
            }
        }

        public Int32 WhitePieceCount
        {
            get
            {
                return this.Model.NumberOfWhitePiece;
            }
        }

        public IEnumerable<ThemeColor> ThemeColorList
        {
            get
            {
                return this.Model.ThemeColorList;
            }
        }


        // ルーム入室時にサーバーから割り振られます。
        public String Id { get; private set; } = String.Empty;

        public Dictionary<Int32, RoomInformationForClient> OthelloRooms { get; private set; } = new Dictionary<Int32, RoomInformationForClient> {
            {0, new RoomInformationForClient(GameMode.VsHuman, 0)}
        };

        private PlayerStatusInSelect _PlayerStatusInSelect = PlayerStatusInSelect.Nothing;
        public PlayerStatusInSelect PlayerStatusInSelect
        {
            get
            {
                return _PlayerStatusInSelect;
            }
            set
            {
                _PlayerStatusInSelect = value;
                this.OnChangeState?.Invoke();
            }
        }

        public Turn MyTurn { get; private set; }
        public Turn CurrentTurn
        {
            get
            {
                return this.Model.Turn;
            }
        }
        public Turn RetiredTurn
        {
            get
            {
                return this.Model.RetiredTurn;
            }
        }

        public event Action OnChangeState = () => { };

        public ClientManager()
        {
            this.Model = this.CreateOhelloModel(ThemeColor.Default);
        }

        public LogOfGame GetLogOfGame(Int32 numberOfLog)
        {
            return this.Model.Log.LogOfGame[numberOfLog];
        }

        public async void UpdateRoomsInformation()
        {
            this.OthelloRooms = await HitApi.FetchRoomsInformationForClient();
            this.OnChangeState?.Invoke();
        }

        public void UpdateNumberOfConnections(IList<(Int32 roomNumber, Int32 numberOfConnection)> numberOfConnectionList)
        {
            foreach (var numberOfConnectionInfo in numberOfConnectionList)
            {
                if (this.OthelloRooms[numberOfConnectionInfo.roomNumber] != null)
                {
                    this.OthelloRooms[numberOfConnectionInfo.roomNumber].NumberOfConnections = numberOfConnectionInfo.numberOfConnection;
                }
            }
        }

        public async Task<Boolean> IsTheRoomSelectable(Int32 roomNumber)
        {
            return 0 < roomNumber && roomNumber <= OthelloRooms.Count
                ? await this.IsSelectRoomFull(roomNumber) == false
                : false;
        }

        private async Task<Boolean> IsSelectRoomFull(Int32 roomNumber)
        {
            var roomInformation = await HitApi.FetchRoomInformationForClient(roomNumber);

            // VsHumanの部屋最大人数は2、VsCpuは1です。
            return roomInformation.GameModeOfRoom == GameMode.VsHuman
                ? roomInformation.NumberOfConnections >= 2
                : roomInformation.NumberOfConnections >= 1;
        }

        public async void FetchID(Int32 roomNumber)
        {
            this.Id = await HitApi.FetchIdentificationNumber(roomNumber);
        }

        public MyOthelloModel CreateOhelloModel(ThemeColor themeColor) {
            var model = new MyOthelloModel(8, themeColor);
            model.GameStateChangedEvent += (state, turn) => this.OnChangeState?.Invoke();
            model.TurnEndEvent += () => this.OnChangeState?.Invoke();
            model.RecreateOthelloSituationEvent += () => this.OnChangeState?.Invoke();
            return model;
        }

        public void RecreateOthelloModel()
        {
            this.Model = this.CreateOhelloModel(this.Model.ThemeColor);
        }

        public async void BuildModeSelectPlayerSituation(Int32 roomNumber, IPlayer player)
        {
            var playerStatus = await HitApi.FetchPlayerStatus(roomNumber, player.Turn, this.Id);

            switch (playerStatus)
            {
                case PlayerStatusInSelect.Waiting:
                    PlayerStatusInSelect = PlayerStatusInSelect.Waiting;
                    this.MyTurn = player.Turn;

                    var polling = new Polling();
                    await polling.WaitOpponent(roomNumber, Id);
                    if (roomNumber == 0)
                    {
                        break;
                    }

                    PlayerStatusInSelect = PlayerStatusInSelect.Nothing;
                    Model.ChangeGameState(GameState.MatchRemaining);
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

        public void ChangeGameState(GameState state)
        {
            this.Model.ChangeGameState(state);
        }

        public void ReCreateOthelloSituation(IEnumerable<LogOfGame> logOfGameList)
        {
            this.Model.ReCreateOthelloSituation(logOfGameList);
        }

        public void RetireProcess(IEnumerable<LogOfGame> logOfGameList)
        {
            this.Model.RetiredTurn = logOfGameList.Last().Point.Y == -1 ? Turn.First : Turn.Second;
            this.ChangeGameState(GameState.MatchRetired);
        }
    }

    public class RoomInformationForClient
    {
        public GameMode GameModeOfRoom { get; private set; }

        public Int32 NumberOfConnections { get; set; }

        public RoomInformationForClient(GameMode gameMode, Int32 numberOfConnections)
        {
            this.GameModeOfRoom = gameMode;
            this.NumberOfConnections = numberOfConnections;
        }
    }

    public enum PlayerStatusInSelect { Nothing, Waiting, CantSelect }
}
