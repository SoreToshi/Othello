using System;
using System.Runtime.CompilerServices;

namespace OthelloClassLibrary.Models {
    public class OthelloManager {
        public static Int32 BoardSize = 8;
        public static MyOthelloModel Instance = new MyOthelloModel(BoardSize, ThemeColor.Default);
    }
}