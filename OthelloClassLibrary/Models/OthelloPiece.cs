namespace OthelloClassLibrary.Models
{
    public enum Side { Black, White }

    public static class SideExtends {
        public static Side ReverseSide(this Side side) {
            return side == Side.White ? Side.Black : Side.White;
        }
    }

    public class OthelloPiece
    {
        public Side Side { get; private set; }

        public OthelloPiece(Side side)
        {
            this.Side = side;
        }

        public void TurnOver()
        {
            if (this.Side == Side.Black)
            {
                this.Side = Side.White;
            }
            else
            {
                this.Side = Side.Black;
            }
        }
    }
}
