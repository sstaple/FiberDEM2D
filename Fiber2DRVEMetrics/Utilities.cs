using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DelaunatorSharp;

namespace MicroCluster
{
    public class Utilities
    {
        // Converts two lists (Y and Z) into an IPoint[].
        public static IPoint[] ConvertListToColumnArray(List<double> Ycoords, List<double> Zcoords)
        {
            int numPoints = Ycoords.Count;
            IPoint[] yzPoints = new IPoint[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                // Here we treat Ycoords as the X values and Zcoords as the Y values.
                // Adjust the mapping if needed.
                yzPoints[i] = new Point(Ycoords[i], Zcoords[i]);
            }
            return yzPoints;
        }

        // Optional: writes triangles to a CSV file (not required for triangulation)
        public static void WriteTrianglesToCsv(IPoint[] points, int[] triangles, string filePath)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filePath))
            {
                sw.WriteLine("TriangleIndex,Point1Index,Point1X,Point1Y,Point2Index,Point2X,Point2Y,Point3Index,Point3X,Point3Y");
                int numTriangles = triangles.Length / 3;
                for (int i = 0; i < numTriangles; i++)
                {
                    int idx0 = triangles[i * 3];
                    int idx1 = triangles[i * 3 + 1];
                    int idx2 = triangles[i * 3 + 2];

                    IPoint p0 = points[idx0];
                    IPoint p1 = points[idx1];
                    IPoint p2 = points[idx2];

                    sw.WriteLine($"{i},{idx0},{p0.X},{p0.Y},{idx1},{p1.X},{p1.Y},{idx2},{p2.X},{p2.Y}");
                }
            }
        }

        public static double ComputeTriangleArea(IPoint[] vertices)
        {
            // Ensure we have exactly three vertices.
            if (vertices.Length != 3)
                throw new ArgumentException("Triangle must have 3 vertices.");

            double y1 = vertices[0].X, z1 = vertices[0].Y;
            double y2 = vertices[1].X, z2 = vertices[1].Y;
            double y3 = vertices[2].X, z3 = vertices[2].Y;

            double area = Math.Abs(y1 * (z2 - z3) + y2 * (z3 - z1) + y3 * (z1 - z2)) / 2.0;
            return area;
        }

        public static double CalculateFiberArea(IPoint[] vertices, double[] fiberRadii)
        {
            if (vertices.Length != 3 || fiberRadii.Length != 3)
                throw new ArgumentException("Expected exactly 3 vertices and 3 fiber radii.");

            double totalFiberArea = 0.0;

            // Loop through each vertex (0,1,2).
            for (int i = 0; i < 3; i++)
            {
                IPoint A = vertices[i];
                IPoint B = vertices[(i + 1) % 3];
                IPoint C = vertices[(i + 2) % 3];

                // Compute vectors AB and AC.
                double ABx = B.X - A.X;
                double ABy = B.Y - A.Y;
                double ACx = C.X - A.X;
                double ACy = C.Y - A.Y;

                // Compute the dot product and the magnitudes.
                double dot = ABx * ACx + ABy * ACy;
                double magAB = Math.Sqrt(ABx * ABx + ABy * ABy);
                double magAC = Math.Sqrt(ACx * ACx + ACy * ACy);

                // Compute the cosine of the angle at A. Clamp to [-1,1] for safety.
                double cosine = dot / (magAB * magAC);
                cosine = Math.Max(-1.0, Math.Min(1.0, cosine));
                double angle = Math.Acos(cosine); // Angle in radians.

                // Compute the area of the circular sector at vertex A.
                // Formula: (1/2) * (radius^2) * angle
                double sectorArea = 0.5 * fiberRadii[i] * fiberRadii[i] * angle;

                totalFiberArea += sectorArea;
            }

            return totalFiberArea;
        }

        public static double CalculateLocalFiberVolumeFraction(IPoint[] vertices, double[] fiberRadii)
        {
            double volumeFraction = 0.0;
            double triArea = ComputeTriangleArea(vertices);
            double fiberArea = CalculateFiberArea(vertices, fiberRadii);

            volumeFraction = fiberArea / triArea;
            return volumeFraction;
        }
        public static double[] MultiThresholdOtsu(double[] values, int numThresholds, int numBins = 256)
        {
            if (numThresholds < 1)
                throw new ArgumentException("numThresholds must be >= 1");
            int N = values.Length;
            if (N == 0)
                return Array.Empty<double>();

            // Compute histogram
            double min = values.Min(), max = values.Max();
            double[] hist = new double[numBins];
            double binWidth = (max - min) / numBins;
            foreach (var v in values)
            {
                int idx = (int)((v - min) / binWidth);
                if (idx < 0) idx = 0;
                else if (idx >= numBins) idx = numBins - 1;
                hist[idx] += 1;
            }
            // Normalize to probabilities
            double[] prob = hist.Select(h => h / N).ToArray();
            double[] binCenters = Enumerable.Range(0, numBins)
                .Select(i => min + (i + 0.5) * binWidth)
                .ToArray();

            // Recursive search for best thresholds
            double bestSigma = -1;
            int[] bestIdx = new int[numThresholds];
            var indices = new int[numThresholds];
            void Recurse(int pos, int start)
            {
                if (pos == numThresholds)
                {
                    // Evaluate between-class variance
                    var classes = new List<(int start, int end)>();
                    int prev = 0;
                    for (int t = 0; t < numThresholds; t++)
                    {
                        classes.Add((prev, indices[t]));
                        prev = indices[t] + 1;
                    }
                    classes.Add((prev, numBins - 1));

                    double globalMean = 0;
                    for (int i = 0; i < numBins; i++) globalMean += binCenters[i] * prob[i];

                    double sigmaB = 0;
                    foreach (var (a, b) in classes)
                    {
                        double w = prob.Skip(a).Take(b - a + 1).Sum();
                        double mu = 0;
                        for (int i = a; i <= b; i++) mu += binCenters[i] * prob[i];
                        if (w > 0) mu /= w;
                        sigmaB += w * (mu - globalMean) * (mu - globalMean);
                    }
                    if (sigmaB > bestSigma)
                    {
                        bestSigma = sigmaB;
                        Array.Copy(indices, bestIdx, numThresholds);
                    }
                    return;
                }
                for (int i = start; i < numBins - (numThresholds - pos); i++)
                {
                    indices[pos] = i;
                    Recurse(pos + 1, i + 1);
                }
            }
            Recurse(0, 0);

            // Convert bestIdx to actual threshold values (bin centers)
            return bestIdx.Select(idx => binCenters[idx]).OrderBy(x => x).ToArray();
        }
        public static double ComputeLowerQuartile(double[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array must not be empty.");

            // Sort the data
            var sorted = values.OrderBy(v => v).ToArray();

            // Position of the 25th percentile
            double pos = 0.25 * (sorted.Length - 1);
            int idx = (int)Math.Floor(pos);
            double frac = pos - idx;

            // Interpolate between neighboring values
            if (idx + 1 < sorted.Length)
                return sorted[idx] + frac * (sorted[idx + 1] - sorted[idx]);

            return sorted[idx];
        }
        public static bool[] IsBelowLowerQuartile(double[] values)
        {
            double q1 = ComputeLowerQuartile(values);
            var mask = new bool[values.Length];
            for (int i = 0; i < values.Length; i++)
                mask[i] = values[i] < q1;
            return mask;
        }
        public static void OutputTiledPoints(string filePath, IPoint[] YZPoints, List<double> tiledR)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("Index,Y,Z,R");
            for (int i = 0; i < YZPoints.Length; i++)
            {
                //double r = i < PackFile.R.Count ? PackFile.R[i] : 0.0;
                writer.WriteLine($"{i},{YZPoints[i].X.ToString(CultureInfo.InvariantCulture)},{YZPoints[i].Y.ToString(CultureInfo.InvariantCulture)},{tiledR[i].ToString(CultureInfo.InvariantCulture)}");
            }
        }

        public static void OutputTriangulationConnectivity(string filePath, Delaunator Triangulation)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("TriangleID,Index0,Index1,Index2");
            int numTriangles = Triangulation.Triangles.Length / 3;
            for (int i = 0; i < numTriangles; i++)
            {
                int idx0 = Triangulation.Triangles[i * 3];
                int idx1 = Triangulation.Triangles[i * 3 + 1];
                int idx2 = Triangulation.Triangles[i * 3 + 2];
                writer.WriteLine($"{i},{idx0},{idx1},{idx2}");
            }
        }

        public static void OutputFilteredTriangles(string filePath, int[] filteredTriangles)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("TriangleID,Index0,Index1,Index2");

            int numTriangles = filteredTriangles.Length / 3;

            for (int i = 0; i < numTriangles; i++)
            {
                int idx0 = filteredTriangles[i * 3];
                int idx1 = filteredTriangles[i * 3 + 1];
                int idx2 = filteredTriangles[i * 3 + 2];
                writer.WriteLine($"{i},{idx0},{idx1},{idx2}");
            }
        }
    }
    public class Point : IPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X}, {Y})";
    }


}
