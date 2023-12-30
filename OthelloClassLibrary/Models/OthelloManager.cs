using System;
using System.Collections.Generic;
using System.Linq;

namespace OthelloClassLibrary.Models
{
    public class OthelloManager
    {

        public static Int32 BoardSize = 8;

        public static IDictionary<Int32, RoomInformationForServer> OthelloRooms = new Dictionary<Int32, RoomInformationForServer>();

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
                this.PlayerInfos.Add(new PlayerAccessInfo((IdentificationNumber)identificationNumber));
            }
        }

        public void RecreateOthello(GameMode gameMode)
        {
            this.Model = new MyOthelloModel(OthelloManager.BoardSize, ThemeColor.Default);
            this.Model.SelectGameMode(gameMode);
        }
    }

    public class PlayerAccessInfo
    {
        public IdentificationNumber IdentificationNumber { get; private set; }
        public Boolean IsPlayerAccess { get; private set; }
        public Int32 PlayerAccessTime { get; private set; }
        public Boolean IsTurnSelected { get; private set; }
        public Turn Turn { get; set; }
        public PlayerAccessInfo(IdentificationNumber identificationNumber)
        {
            this.IdentificationNumber = identificationNumber;
        }

        public void InvertIsAccess()
        {
            this.IsPlayerAccess = !this.IsPlayerAccess;
        }
        public void AddAccessTime()
        {
            // 接続している限り増え続けていくのでMaxValueに到達したら0に戻します。
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

    public enum IdentificationNumber { One, Two }
}