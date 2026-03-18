using System;

namespace FxTMeshGenerator.Geometry
{
    /// <summary>
    /// Mathematical utility methods for mesh generation.
    /// </summary>
    public static class MathHelper
    {
        private const double OverlapTolerance = 1e-5;

        /// <summary>
        /// Creates a 2D vector from point p1 to point p2.
        /// </summary>
        public static Point2D MakeVector2D(Point2D p1, Point2D p2)
        {
            return new Point2D(p2.X - p1.X, p2.Y - p1.Y);
        }

        /// <summary>
        /// Calculates the angle between two vectors, measured counter-clockwise from the positive x-axis.
        /// </summary>
        public static double CalculateAngleBetweenVectors(Point2D vec1, Point2D vec2)
        {
            double dotProduct = vec1.X * vec2.X + vec1.Y * vec2.Y;
            double norm1 = Math.Sqrt(vec1.X * vec1.X + vec1.Y * vec1.Y);
            double norm2 = Math.Sqrt(vec2.X * vec2.X + vec2.Y * vec2.Y);

            double theta = Math.Acos(dotProduct / (norm1 * norm2));

            // Check for vectors pointing downward (negative y component)
            Point2D unit = new Point2D(1, 0);
            bool vec1IsUnit = Math.Abs(vec1.X - 1.0) < 1e-10 && Math.Abs(vec1.Y) < 1e-10;
            bool vec2IsUnit = Math.Abs(vec2.X - 1.0) < 1e-10 && Math.Abs(vec2.Y) < 1e-10;

            if ((vec1IsUnit && vec2.Y < 0) || (vec2IsUnit && vec1.Y < 0))
            {
                theta = 2.0 * Math.PI - theta;
            }

            return theta;
        }

        /// <summary>
        /// Calculates Euclidean distance between two points.
        /// </summary>
        public static double CalcDistanceBetweenTwoPoints(Point2D p1, Point2D p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Checks if two points overlap within tolerance.
        /// </summary>
        public static bool OverlapCheck(Point2D p1, Point2D p2)
        {
            return CalcDistanceBetweenTwoPoints(p1, p2) <= OverlapTolerance;
        }
    }
}
