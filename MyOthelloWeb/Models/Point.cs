using System.Reflection.Metadata;

namespace MyOthelloWeb.Models
{
    public class Point
    {
        public Int32 X { get; private set; }
        public Int32 Y { get; private set; }

        public static Point operator +(Point point, Vector vector)
        {
            return new Point(point.X + vector.X, point.Y + vector.Y);
        }

        public Point(Int32 x, Int32 y)
        {
            this.X = x;
            this.Y = y;
        }

        public Point(Point point)
        {
            this.X = point.X;
            this.Y = point.Y;
        }

        public Point Plus(Vector vector)
        {
            return new Point(this.X + vector.X, this.Y + vector.Y);
        }
    }

    public class Vector
    {
        public Int32 X { get; private set; }
        public Int32 Y { get; private set; }

        public Vector(Int32 x, Int32 y)
        {
            this.X = x;
            this.Y = y;
        }
    }
    public class ComparePoint : IEqualityComparer<Point>
    {
        public bool Equals(Point point, Point comparePoint)
        {
            if (point.X == comparePoint.X &&
                point.Y == comparePoint.Y)
            {
                return true;
            }
            return false;
        }
        public int GetHashCode(Point point)
        {
            return point.X ^ point.Y.GetHashCode();
        }
    }
}
