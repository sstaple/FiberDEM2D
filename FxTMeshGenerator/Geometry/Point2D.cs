
namespace FxTMeshGenerator.Geometry
{
    public readonly struct Point2D
    {
        public double X { get; }
        public double Y { get; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Point2D operator +(Point2D a, Point2D b) => new Point2D(a.X + b.X, a.Y + b.Y);
        public static Point2D operator -(Point2D a, Point2D b) => new Point2D(a.X - b.X, a.Y - b.Y);
        public static Point2D operator *(double s, Point2D a) => new Point2D(s * a.X, s * a.Y);

        public override string ToString() => $"({X}, {Y})";
    }
}
