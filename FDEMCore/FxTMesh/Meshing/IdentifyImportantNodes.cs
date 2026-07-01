using FDEMCore.FxTMesh.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDEMCore.FxTMesh.Meshing
{
    public static class IdentifyImportantNodes
    {

        public static PeriodicBoundaryData BuildPeriodicBoundaryData(CellBoundary boundary, double tol, 
            List<Point2D> _globalNodes)
        {
            var result = new PeriodicBoundaryData();
            var pairKeys = new HashSet<string>();

            double lx = boundary.ODimensions[1];
            double ly = boundary.ODimensions[2];

            bool xPeriodic = boundary.Walls[2].BoundaryType == BoundaryType.Periodic;
            bool yPeriodic = boundary.Walls[4].BoundaryType == BoundaryType.Periodic;

            var nodeLookup = BuildGlobalNodeLookup(tol, _globalNodes);

            for (int i = 0; i < _globalNodes.Count; i++)
            {
                var node = _globalNodes[i];

                if (xPeriodic)
                {
                    if (TryAddPeriodicPair(i, new Point2D(node.X + lx, node.Y), nodeLookup, result.Pairs,
                        pairKeys, tol, out int matchingIndex, _globalNodes))
                    {
                        result.X1Nodes.Add(matchingIndex);
                    }
                }

                if (yPeriodic)
                {
                    if (TryAddPeriodicPair(i, new Point2D(node.X, node.Y + ly), nodeLookup,
                        result.Pairs, pairKeys, tol, out int matchingIndex, _globalNodes))
                    {
                        result.Y1Nodes.Add(matchingIndex);
                    }
                }
            }

            if (xPeriodic || yPeriodic)
            {
                result.PinnedNode = FindPinnedNode(tol, _globalNodes);
            }

            return result;
        }
        private static Dictionary<string, List<int>> BuildGlobalNodeLookup(double tolerance, List<Point2D> _globalNodes)
        {
            var lookup = new Dictionary<string, List<int>>();

            for (int i = 0; i < _globalNodes.Count; i++)
            {
                string key = GetRoundedPointKey(_globalNodes[i], tolerance);

                if (!lookup.TryGetValue(key, out var indices))
                {
                    indices = new List<int>();
                    lookup[key] = indices;
                }

                indices.Add(i);
            }

            return lookup;
        }

        private static bool TryAddPeriodicPair(int nodeIndex, Point2D projectedPoint, Dictionary<string, List<int>> nodeLookup,
            List<(int, int)> pairs, HashSet<string> pairKeys, double tolerance, out int matchingNodeIndex, List<Point2D> _globalNodes)
        {
            matchingNodeIndex = -1;
            int? match = FindGlobalNodeIndex(projectedPoint, nodeLookup, tolerance, _globalNodes);

            if (!match.HasValue)
                return false;

            int i = nodeIndex;
            int j = match.Value;

            if (i == j)
                return false;

            string pairKey = $"{Math.Min(i, j)}_{Math.Max(i, j)}";

            if (!pairKeys.Add(pairKey))
                return false;

            pairs.Add((i, j));
            matchingNodeIndex = j;
            return true;
        }
        private static int? FindGlobalNodeIndex(Point2D target, Dictionary<string, List<int>> nodeLookup, double tolerance,
            List<Point2D> _globalNodes)
        {
            string key = GetRoundedPointKey(target, tolerance);

            if (!nodeLookup.TryGetValue(key, out var candidateIndices))
                return null;

            foreach (int candidateIndex in candidateIndices)
            {
                var candidate = _globalNodes[candidateIndex];

                double dx = candidate.X - target.X;
                double dy = candidate.Y - target.Y;

                if (Math.Sqrt(dx * dx + dy * dy) < tolerance)
                    return candidateIndex;
            }

            return null;
        }

        private static string GetRoundedPointKey(Point2D point, double tolerance)
        {
            long ix = (long)Math.Round(point.X / tolerance);
            long iy = (long)Math.Round(point.Y / tolerance);

            return $"{ix}_{iy}";
        }

        private static int? FindPinnedNode(double tolerance, List<Point2D> _globalNodes)
        {
            int? bestIndex = null;
            double bestDistanceSquared = double.MaxValue;

            // Find the node closest to the origin (0,0) 
            for (int i = 0; i < _globalNodes.Count; i++)
            {
                var node = _globalNodes[i];

                double d2 = node.X * node.X + node.Y * node.Y;

                if (d2 < bestDistanceSquared)
                {
                    bestDistanceSquared = d2;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
    #region Helper Classes

    public sealed class PeriodicBoundaryData
    {
        public List<(int Node1, int Node2)> Pairs { get; } = new();
        public List<int> X1Nodes { get; } = new();
        public List<int> Y1Nodes { get; } = new();
        public int? PinnedNode { get; set; }
    }

    #endregion
}
