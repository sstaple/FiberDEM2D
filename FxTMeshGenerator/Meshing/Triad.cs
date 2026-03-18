using System;
using System.Collections.Generic;
using System.Linq;
using FxTMeshGenerator.Geometry;
using FDEMCore;

namespace FxTMeshGenerator.Meshing
{
    /// <summary>
    /// Represents a triangle in the mesh formed by three fiber centers.
    /// </summary>
    public sealed class Triad
    {
        public int Number { get; }
        public IReadOnlyList<Fiber> Fibers { get; private set; }
        public Point2D[] NodePositions { get; private set; }
        public int[,] Edges { get; private set; }
        public int[] FibersWhichOverlapTriad { get; private set; }

        public Triad(int number, IReadOnlyList<Fiber> fibers, Point2D[] nodePositions)
        {
            if (fibers == null || fibers.Count != 3)
                throw new ArgumentException("Triad must have exactly 3 fibers.", nameof(fibers));
            if (nodePositions == null || nodePositions.Length != 3)
                throw new ArgumentException("Triad must have exactly 3 node positions.", nameof(nodePositions));

            Number = number;
            Fibers = fibers;
            NodePositions = nodePositions;
            Edges = new int[3, 2];
            FibersWhichOverlapTriad = new int[3];

            InitializeEdges();
        }

        /// <summary>
        /// Initializes the three edges of the triad based on fiber numbers.
        /// Edge i connects two of the three fibers.
        /// </summary>
        private void InitializeEdges()
        {
            // Edges stored as pairs of fiber indices (0-based within the mesh)
            // We'll use a convention similar to MATLAB: we need fiber numbers, not local indices
            // For now, assume Fibers have a Number property or we track separately
            // Based on MATLAB: Edges(1,:) = [Fiber1.Number, Fiber2.Number], etc.
            
            // We need to get fiber numbers - let's assume we track this in the mesh
            // For now, we'll store local indices and fix during integration
            Edges[0, 0] = 0; Edges[0, 1] = 1; // Fiber 1 to Fiber 2
            Edges[1, 0] = 0; Edges[1, 1] = 2; // Fiber 1 to Fiber 3
            Edges[2, 0] = 1; Edges[2, 1] = 2; // Fiber 2 to Fiber 3
        }

        /// <summary>
        /// Updates edge definitions with actual fiber indices from the mesh.
        /// </summary>
        public void SetEdgesWithFiberIndices(int fiberIndex0, int fiberIndex1, int fiberIndex2)
        {
            Edges[0, 0] = fiberIndex0; Edges[0, 1] = fiberIndex1;
            Edges[1, 0] = fiberIndex0; Edges[1, 1] = fiberIndex2;
            Edges[2, 0] = fiberIndex1; Edges[2, 1] = fiberIndex2;
        }

        /// <summary>
        /// Determines if any fiber overlaps with the triad's interior.
        /// Returns true if overlap is detected.
        /// </summary>
        public bool DetermineIfFibersOverlapTriad()
        {
            var fiberA = Fibers[0];
            var fiberB = Fibers[1];
            var fiberC = Fibers[2];

            // Use actual node positions (which include projection offsets) instead of original fiber positions
            var fA = NodePositions[0];
            var fB = NodePositions[1];
            var fC = NodePositions[2];

            // Create vectors between fibers
            var vAB = MathHelper.MakeVector2D(fA, fB);
            var vBA = MathHelper.MakeVector2D(fB, fA);
            var vAC = MathHelper.MakeVector2D(fA, fC);
            var vCA = MathHelper.MakeVector2D(fC, fA);
            var vBC = MathHelper.MakeVector2D(fB, fC);
            var vCB = MathHelper.MakeVector2D(fC, fB);

            // Calculate angles between vectors
            double tAB_AC = MathHelper.CalculateAngleBetweenVectors(vAB, vAC);
            double tBA_BC = MathHelper.CalculateAngleBetweenVectors(vBA, vBC);
            double tCA_CB = MathHelper.CalculateAngleBetweenVectors(vCA, vCB);

            // Calculate angles to x-axis
            var unit = new Point2D(1, 0);
            double t1_A = MathHelper.CalculateAngleBetweenVectors(vAB, unit);
            double t3_A = t1_A + tAB_AC;
            double t1_B = MathHelper.CalculateAngleBetweenVectors(vBC, unit);
            double t3_B = t1_B + tBA_BC;
            double t1_C = MathHelper.CalculateAngleBetweenVectors(vCA, unit);
            double t3_C = t1_C + tCA_CB;

            // Calculate angle representing shortest distance to opposite base
            double tDiff_ABase = -Math.Atan((fB.X - fC.X) / (fB.Y - fC.Y));
            double tDiff_BBase = -Math.Atan((fC.X - fA.X) / (fC.Y - fA.Y));
            double tDiff_CBase = -Math.Atan((fA.X - fB.X) / (fA.Y - fB.Y));

            // Calculate minimum distances
            double dMin_A = CalculateMinDistanceBetweenFiberAndOppositeBase(tDiff_ABase, t1_A, t3_A, fA, fB, fC);
            double dMin_B = CalculateMinDistanceBetweenFiberAndOppositeBase(tDiff_BBase, t1_B, t3_B, fB, fC, fA);
            double dMin_C = CalculateMinDistanceBetweenFiberAndOppositeBase(tDiff_CBase, t1_C, t3_C, fC, fA, fB);

            // Check for overlaps (using factor of 20 as in MATLAB)
            const double overlapFactor = 20.0;
            DoesFiberHaveOverlapWithTriad(fiberA, dMin_A, 0, overlapFactor);
            DoesFiberHaveOverlapWithTriad(fiberB, dMin_B, 1, overlapFactor);
            DoesFiberHaveOverlapWithTriad(fiberC, dMin_C, 2, overlapFactor);

            bool hasOverlap = FibersWhichOverlapTriad.Any(x => x != 0);

            if (hasOverlap)
            {
                Console.WriteLine($"    DEBUG: Triad {Number} - dMin=[{dMin_A:F4}, {dMin_B:F4}, {dMin_C:F4}], " +
                                  $"radii=[{fiberA.Radius:F4}, {fiberB.Radius:F4}, {fiberC.Radius:F4}], " +
                                  $"overlaps=[{FibersWhichOverlapTriad[0]}, {FibersWhichOverlapTriad[1]}, {FibersWhichOverlapTriad[2]}]");
            }

            return hasOverlap;
        }

        /// <summary>
        /// Checks if a specific fiber overlaps with the triad.
        /// </summary>
        private void DoesFiberHaveOverlapWithTriad(Fiber fiber, double fiberToBaseDist, int index, double factor)
        {
            double minDist = fiber.Radius + fiber.Radius / factor;
            FibersWhichOverlapTriad[index] = (fiberToBaseDist <= minDist) ? 1 : 0;
        }

        /// <summary>
        /// Calculates minimum distance between a fiber and the opposite edge of the triad.
        /// </summary>
        private static double CalculateMinDistanceBetweenFiberAndOppositeBase(
            double tDiff, double t1, double t3, Point2D p1, Point2D p2, Point2D p3)
        {
            double xa = p1.X, ya = p1.Y;
            double xb = p2.X, yb = p2.Y;
            double xc = p3.X, yc = p3.Y;
            
            double tDiff_pi = tDiff + Math.PI;
            double tDiff_2pi = tDiff + 2.0 * Math.PI;
            
            double t;
            if (tDiff >= t1 && tDiff <= t3)
                t = tDiff;
            else if (tDiff_pi >= t1 && tDiff_pi <= t3)
                t = tDiff_pi;
            else if (tDiff_2pi >= t1 && tDiff_2pi <= t3)
                t = tDiff_2pi;
            else
            {
                // Shortest distance is at one of the edges
                double d1 = CalculateDistance(xa, ya, xb, yb, xc, yc, t1);
                double d3 = CalculateDistance(xa, ya, xb, yb, xc, yc, t3);
                t = (d1 <= d3) ? t1 : t3;
            }
            
            return CalculateDistance(xa, ya, xb, yb, xc, yc, t);
        }

        /// <summary>
        /// Helper method to calculate distance formula used in MATLAB.
        /// </summary>
        private static double CalculateDistance(double xa, double ya, double xb, double yb, double xc, double yc, double t)
        {
            double tanT = Math.Tan(t);
            double slope = (yb - yc) / (xb - xc);
            
            double xPart = xa + (ya - yb - xa * tanT + (xb * (yb - yc)) / (xb - xc)) / (tanT - slope);
            double yPart = ya - (tanT * (yb - (xb * (yb - yc)) / (xb - xc)) - ((ya - xa * tanT) * (yb - yc)) / (xb - xc)) / (tanT - slope);
            
            return Math.Sqrt(xPart * xPart + yPart * yPart);
        }

        /// <summary>
        /// Checks if two fiber indices are part of this triad.
        /// </summary>
        public static bool IsFiberPairInTriad(int[] pair, int[] triadFiberIndices)
        {
            return pair.Intersect(triadFiberIndices).Count() == 2;
        }

        /// <summary>
        /// Updates the fibers in this triad (used during retriangulation).
        /// </summary>
        public void UpdateFibers(IReadOnlyList<Fiber> newFibers)
        {
            if (newFibers == null || newFibers.Count != 3)
                throw new ArgumentException("Must provide exactly 3 fibers.", nameof(newFibers));
            
            Fibers = newFibers;
            InitializeEdges();
        }
    }
}