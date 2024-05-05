using OthelloClassLibrary.Models;

namespace MyOthelloWeb.Models
{
    public static class OthelloManager
    {
        public static readonly Int32 BoardSize = 8;

        private static IDictionary<Int32, RoomInformationForServer> OthelloRooms = new Dictionary<Int32, RoomInformationForServer>();

        static OthelloManager()
        {
            // ここで部屋の数を決めています。
            var numberOfRooms = 6;

            // 部屋の番号を1から始めたいのでroomNumberを1から開始しています。
            for (var roomNumber = 1; roomNumber <= numberOfRooms; roomNumber++)
            {
                var gameMode = numberOfRooms / 2 >= roomNumber ? GameMode.VsHuman : GameMode.VsCpu;

                OthelloRooms.Add(roomNumber, new RoomInformationForServer(gameMode));
            }
        }

        public static IList<KeyValuePair<Int32, RoomInformationForServer>> Rooms {
            get {
                return OthelloRooms.ToList();
            }
        }

        public static RoomInformationForServer? GetRoom(Int32 roomNumber) { 
            return OthelloRooms[roomNumber];
        }

        public static void RecreateRoomInformationForServer(Int32 roomNumber)
        {
            var gameMode = OthelloRooms[roomNumber].Model.GameMode;
            OthelloRooms[roomNumber] = new RoomInformationForServer(gameMode);
        }
    }

    public class RoomInformationForServer
    {
        public MyOthelloModel Model { get; private set; }
        public IList<PlayerAccessInfo> PlayerInfos { get; } = new List<PlayerAccessInfo>();
        public Int32 NumberOfConnection
        {
            get
            {
                return PlayerInfos.Where((info) => (info.IsPlayerAccess)).Count();
            }
        }

        public RoomInformationForServer(GameMode gameMode)
        {
            this.Model = new MyOthelloModel(OthelloManager.BoardSize, ThemeColor.Default);
            this.Model.SelectGameMode(gameMode);

            Int32 identificationNumber;
            var maxCapacity = gameMode == GameMode.VsCpu ? 1 : 2;
            for (identificationNumber = 0; identificationNumber < maxCapacity; identificationNumber++)
            {
                this.PlayerInfos.Add(new PlayerAccessInfo());
            }
        }

        public void RecreateOthello(GameMode gameMode)
        {
            this.Model = new MyOthelloModel(OthelloManager.BoardSize, ThemeColor.Default);
            this.Model.SelectGameMode(gameMode);
        }

        /// <exception cref="InvalidOperationException"></exception>
        public String FindAvailableIdentificationNumber()
        {
            return this.PlayerInfos.First((playerInfo) => playerInfo.IsPlayerAccess == false).ID;
        }
    }

    public class PlayerAccessInfo
    {
        public String ID { get; private set; }
        public Boolean IsPlayerAccess { get; private set; }
        public Int32 PlayerAccessTime { get; private set; }
        public Boolean IsTurnSelected { get; private set; }
        public Turn Turn { get; set; }
        // コンストラクタでIDをランダムに生成します。
        public PlayerAccessInfo()
        {
            this.ID = Guid.NewGuid().ToString("N");
        }

        public void InvertIsAccess()
        {
            this.IsPlayerAccess = !this.IsPlayerAccess;
        }
        public void AddAccessTime()
        {
            // 接続している限り増え続けていくのでMaxValue前に到達したら0に戻します。
            if (Int32.MaxValue - 1 < this.PlayerAccessTime)
            {
                this.PlayerAccessTime = 0;
            }
            this.PlayerAccessTime = this.PlayerAccessTime + 1;
        }

        public void InvertIsTurnSelected()
        {
            this.IsTurnSelected = !this.IsTurnSelected;
        }
    }

}