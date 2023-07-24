using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;
using MyOthelloWeb.Models;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MyOthelloWeb.Controllers;

namespace MyOthelloWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly Int32 BoardSize;
        private MyOthelloModel Othello;
        private readonly IJSRuntime JS;

        public ThemeColor ThemeColor
        {
            get
            {
                return Othello.ThemeColor;
            }
        }
        public Turn Turn
        {
            get
            {
                return Othello.Turn;
            }
        }
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
        public IList<LogOfGame> LogOfGame
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
        public Int32 CountOfEmptyPoint { 
            get
            {
                var count = 0;
                foreach(var piece in this.Othello.Pieces)
                {
                    if (piece == null)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public IEnumerable<Tuple<OthelloPiece, Int32>> Pieces
        {
            get
            {
                return this.Othello.Pieces.Select((piece, i) => Tuple.Create(piece, i));
            }
        }


        public IndexModel()
        {
            this.BoardSize = 8;
            this.Othello = OthelloManager.Instance;
        }



        public String ThemeColorString(ThemeColor themeColor)
        {
            switch (themeColor)
            {
                case ThemeColor.Default:
                    return "デフォルト";
                case ThemeColor.Dango:
                    return "ダンゴ";
                case ThemeColor.Sakura:
                    return "サクラ";
                case ThemeColor.Ice:
                    return "アイス";
                default:
                    return "存在しません";
            }
        }
        public void RestartOthello()
        {
            this.Othello = new MyOthelloModel(this.BoardSize, Othello.ThemeColor);
        }


        public async Task<String> ReadLogInfo(InputFileChangeEventArgs e)
        {
            var file = e.GetMultipleFiles(1);
            var buf = new byte[file[0].Size];
            await file[0].OpenReadStream().ReadAsync(buf);
            var logInfoString = System.Text.Encoding.UTF8.GetString(buf);
            return logInfoString;
        }

        public async Task DownloadFileFromStream(Stream logStream)
        {
            var fileName = "savedlog.txt";

            using var streamRef = new DotNetStreamReference(stream: logStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
        //public void RetireMatch()
        //{
        //    OthelloManager.Instance.ChangeGameState(GameState.MatchRetired);
        //}
    }
}