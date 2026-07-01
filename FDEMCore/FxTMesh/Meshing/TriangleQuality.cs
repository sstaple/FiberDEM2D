using FDEMCore.FxTMesh.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FDEMCore.FxTMesh.Meshing
{
    #region helper Classes
    public sealed class FiberOverlapInfo
    {
        public bool HasCriticalOverlap { get; set; }
        public bool NeedsAdjustment { get; set; }
    }
   
    #endregion
    public sealed class TriangleQuality
    {
        private static double triangleToFiberSpacingThreshold = 0.01;
        private static double triangleElementToFiberSpacingThreshold = 0.5;
        public int Inversions { get; init; }
        public int CriticalOverlaps { get; init; }
        public int AdjustableOverlaps { get; init; }
        public double InnerAspectRatio { get; init; }
        public double OuterAspectRatio { get; init; }
        public Point2D[] InteriorPoints { get; init; }
        public FiberOverlapInfo[] FiberOverlaps { get; init; }
        public bool HasCriticalOverlap => CriticalOverlaps > 0;

        public TriangleQuality(Node[] triangleNodes, IReadOnlyList<Fiber> fibers)
        {
            FiberOverlaps = DetectFiberOverlaps(triangleNodes, fibers);
            InteriorPoints = BuildInteriorPoints(triangleNodes, fibers);

            int inversions = 0;

            for (int i = 0; i < 3; i++)
            {
                var currentNode = triangleNodes[i];

                if (!currentNode.FiberId.HasValue)
                    continue;

                var fiber = fibers[currentNode.FiberId.Value];
                var other = GetOtherIndices(i);

                var fiberCenter = currentNode.P;
                var surfacePoint = InteriorPoints[i];

                var outerPoint1 = triangleNodes[other[0]].P;
                var outerPoint2 = triangleNodes[other[1]].P;

                bool inversion = !SameSide(fiberCenter, surfacePoint, outerPoint1, outerPoint2);
                if (inversion)
                    inversions++;

                double distanceToOuterEdge = CalculatePointToLineDistance(fiberCenter, outerPoint1, outerPoint2);

            }

            Inversions = inversions;
            CriticalOverlaps = FiberOverlaps.Count(o => o.HasCriticalOverlap);
            AdjustableOverlaps = FiberOverlaps.Count(o => o.NeedsAdjustment);

            InnerAspectRatio = CalculateAspectRatio(InteriorPoints[0], InteriorPoints[1], InteriorPoints[2]);
            OuterAspectRatio = CalculateAspectRatio(triangleNodes[0].P, triangleNodes[1].P, triangleNodes[2].P);

        }

        public TriangleQuality()
        {
            InteriorPoints = Array.Empty<Point2D>();
            FiberOverlaps = Array.Empty<FiberOverlapInfo>();
        }

        #region helperMethods

        public static Point2D[] BuildInteriorPoints(Node[] triangleNodes, IReadOnlyList<Fiber> fibers)
        {
            var interiorPoints = new Point2D[3];

            for (int i = 0; i < 3; i++)
            {
                var currentNode = triangleNodes[i];

                if (!currentNode.FiberId.HasValue)
                {
                    interiorPoints[i] = currentNode.P;
                    continue;
                }

                var fiber = fibers[currentNode.FiberId.Value];
                var other = GetOtherIndices(i);

                interiorPoints[i] = CalculateFiberSurfacePoint(currentNode.P, fiber.Radius, triangleNodes[other[0]].P, triangleNodes[other[1]].P);
            }

            return interiorPoints;
        }

        public static FiberOverlapInfo[] DetectFiberOverlaps(Node[] triangleNodes, IReadOnlyList<Fiber> fibers)
        {
            var overlapInfo = new FiberOverlapInfo[3];

            for (int i = 0; i < 3; i++)
            {
                overlapInfo[i] = new FiberOverlapInfo();

                var currentNode = triangleNodes[i];

                if (!currentNode.FiberId.HasValue)
                    continue;

                var fiber = fibers[currentNode.FiberId.Value];
                var other = GetOtherIndices(i);

                double distanceToOuterEdge = CalculatePointToLineDistance(currentNode.P, triangleNodes[other[0]].P, triangleNodes[other[1]].P);

                overlapInfo[i].HasCriticalOverlap = distanceToOuterEdge <= fiber.Radius + fiber.Radius * triangleToFiberSpacingThreshold;
                overlapInfo[i].NeedsAdjustment = distanceToOuterEdge <= fiber.Radius + fiber.Radius * triangleElementToFiberSpacingThreshold;
            }

            return overlapInfo;
        }

        private static Point2D CalculateFiberSurfacePoint(Point2D fiberCenter, double fiberRadius, Point2D otherPoint1, Point2D otherPoint2)
        {
            var vec1 = Normalize(MathHelper.MakeVector2D(fiberCenter, otherPoint1));
            var vec2 = Normalize(MathHelper.MakeVector2D(fiberCenter, otherPoint2));

            var direction = Normalize(new Point2D(vec1.X + vec2.X, vec1.Y + vec2.Y));

            return new Point2D(fiberCenter.X + fiberRadius * direction.X, fiberCenter.Y + fiberRadius * direction.Y);
        }

        private static int[] GetOtherIndices(int currentIndex)
        {
            return currentIndex switch
            {
                0 => new[] { 1, 2 },
                1 => new[] { 0, 2 },
                2 => new[] { 0, 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(currentIndex))
            };
        }

        private static double CalculatePointToLineDistance(Point2D point, Point2D lineStart, Point2D lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;

            if (Math.Abs(dx) < 1e-10 && Math.Abs(dy) < 1e-10)
                return MathHelper.CalcDistanceBetweenTwoPoints(point, lineStart);

            double numerator = Math.Abs(dy * point.X - dx * point.Y + lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X);
            double denominator = Math.Sqrt(dx * dx + dy * dy);

            return numerator / denominator;
        }

        private static double CalculateAspectRatio(Point2D p1, Point2D p2, Point2D p3)
        {
            double edge1 = MathHelper.CalcDistanceBetweenTwoPoints(p1, p2);
            double edge2 = MathHelper.CalcDistanceBetweenTwoPoints(p2, p3);
            double edge3 = MathHelper.CalcDistanceBetweenTwoPoints(p3, p1);

            double maxEdge = Math.Max(edge1, Math.Max(edge2, edge3));
            double minEdge = Math.Min(edge1, Math.Min(edge2, edge3));

            if (minEdge < 1e-10)
                return double.MaxValue;

            return maxEdge / minEdge;
        }

        private static bool SameSide(Point2D p1, Point2D pref, Point2D p2, Point2D p3, double tol = 1e-12)
        {
            double s1 = Side(p2, p3, p1);
            double s2 = Side(p2, p3, pref);

            return s1 * s2 > tol;
        }

        private static double Side(Point2D p2, Point2D p3, Point2D q)
        {
            return (p3.X - p2.X) * (q.Y - p2.Y) - (p3.Y - p2.Y) * (q.X - p2.X);
        }

        private static Point2D Normalize(Point2D vector)
        {
            double length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);

            if (length < 1e-10)
                return new Point2D(1, 0);

            return new Point2D(vector.X / length, vector.Y / length);
        }
        #endregion
    }


}
